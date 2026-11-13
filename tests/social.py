"""Real authenticated groups/chat/trade/shared-kill protocol integration."""
import asyncio,json,os,secrets,urllib.request
import websockets
BASE=os.environ.get('TEST_AUTH_URL','http://127.0.0.1:5081')
def api(path,body,token=''):
    h={'Content-Type':'application/json'}
    if token:h['Authorization']='Bearer '+token
    with urllib.request.urlopen(urllib.request.Request(BASE+path,data=json.dumps(body).encode(),headers=h),timeout=10) as r:return json.load(r)
async def main():
    sockets=[];states={};seq={};ids={};readers=[]
    async def wait(test):
        async with asyncio.timeout(12):
            while not test():await asyncio.sleep(.02)
    async def send(name,kind,target='',text=''):
        seq[name]+=1;await sockets[ord(name)-65].send(json.dumps(dict(type='action',seq=seq[name],kind=kind,target=target,text=text)))
    async def watch(name,ws):
        async for raw in ws:
            value=json.loads(raw)
            if value.get('type')=='snapshot':states[name]=value
    async def go(name,x,z):
        for _ in range(240):
            s=states[name];p=next(p for p in s['players'] if p['id']==ids[name]);dx,dz=x-p['x'],z-p['z'];seq[name]+=1
            if (dx*dx+dz*dz)**.5<.2:
                await sockets[ord(name)-65].send(json.dumps(dict(type='input',seq=seq[name],x=0,z=0)));return
            await sockets[ord(name)-65].send(json.dumps(dict(type='input',seq=seq[name],x=dx,z=dz)))
            await wait(lambda:states[name]['tick']>s['tick'])
        raise AssertionError('navigation timeout')
    try:
        for name in 'ABC':
            suffix=secrets.token_hex(5);token=api('/api/register',dict(username='s_'+suffix,password='Social-Test-Only-Password-123!'))['token']
            c=api('/api/characters',dict(slot=0,name='Spieler'+name,classId='Shield Sword'),token);ticket=api('/api/ticket',dict(characterId=c['id']),token)['ticket']
            ws=await websockets.connect(BASE.replace('http','ws',1)+'/ws');sockets.append(ws);seq[name]=0
            await ws.send(json.dumps(dict(type='auth',ticket=ticket)));welcome=json.loads(await ws.recv());assert welcome['version']==9;ids[name]=welcome['id'];readers.append(asyncio.create_task(watch(name,ws)))
        await wait(lambda:len(states)==3)
        await send('A','party_invite',ids['B']);await wait(lambda:len(states['B']['social']['invitations'])==1)
        await send('B','party_accept',ids['A']);await wait(lambda:states['A']['social']['party'] is not None and len(states['A']['social']['party']['members'])==2)
        await send('A','chat_private',ids['B'],'Nur B darf das lesen')
        await wait(lambda:any(c['text']=='Nur B darf das lesen' for c in states['B']['social']['chat']))
        assert not any(c['text']=='Nur B darf das lesen' for c in states['C']['social']['chat'])
        tick=states['A']['tick'];await wait(lambda:states['A']['tick']>tick+16)
        await send('A','chat_party','','Gruppenkanal')
        await wait(lambda:any(c['text']=='Gruppenkanal' for c in states['B']['social']['chat']))
        assert not any(c['text']=='Gruppenkanal' for c in states['C']['social']['chat'])
        await send('A','trade_open',ids['B']);await wait(lambda:states['B']['social']['trade'] is not None)
        await send('A','trade_offer','','potion|1|0');await send('B','trade_offer','','|0|5')
        await wait(lambda:any(o['coins']==5 for o in states['A']['social']['trade']['offers']))
        await send('A','trade_confirm');await wait(lambda:any(o['owner']==ids['A'] and o['confirmed'] for o in states['A']['social']['trade']['offers']))
        assert states['A']['self']['coins']==20
        await send('B','trade_confirm');await wait(lambda:states['A']['self']['coins']==25 and states['B']['self']['coins']==15)
        assert states['A']['self']['character']['inventory']['potion']==1 and states['B']['self']['character']['inventory']['potion']==3
        assert states['C']['social']['trade'] is None
        await go('A',-3,7);await send('A','interact');await go('B',-3,7);await send('B','interact')
        await go('A',-6,-6);await go('B',-5,-6)
        await send('A','target','drone-1');await wait(lambda:next(p for p in states['A']['players'] if p['id']==ids['A'])['target']=='drone-1')
        await send('A','skill','skill1');await wait(lambda:states['A']['self']['character']['skills'][0]['cooldown']>0)
        await wait(lambda:states['A']['self']['character']['skills'][0]['cooldown']==0)
        await send('A','skill','skill1');await wait(lambda:states['A']['self']['exp']==10 and states['B']['self']['exp']==10)
        assert states['A']['self']['kills']==states['B']['self']['kills']==1
        assert len(states['A']['loot'])==len(states['B']['loot'])==1
        assert states['A']['loot'][0]['id']!=states['B']['loot'][0]['id'] and states['C']['self']['exp']==0
        print('PASS: real authenticated party, private/group chat, two-confirmation trade, shared EXP and personal party drops')
    finally:
        for ws in sockets:await ws.close()
        for task in readers:task.cancel()
asyncio.run(main())
