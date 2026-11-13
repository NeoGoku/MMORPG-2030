"""Public API/real websocket housing, shared furniture, guild and auction journey. No test-only game API."""
import asyncio,json,os,secrets,urllib.request
import websockets
BASE=os.environ.get('TEST_AUTH_URL','http://127.0.0.1:5081')
def api(path,body=None,token=''):
    headers={'Content-Type':'application/json'}
    if token:headers['Authorization']='Bearer '+token
    req=urllib.request.Request(BASE+path,data=json.dumps(body).encode() if body is not None else None,headers=headers)
    with urllib.request.urlopen(req,timeout=10) as r:return json.load(r)
async def main():
    sockets={};states={};seq={};ids={};tokens={};tasks=[];sent={}
    async def wait(test,timeout=15):
        async with asyncio.timeout(timeout):
            while not test():await asyncio.sleep(.025)
    async def watch(name,ws):
        async for raw in ws:
            value=json.loads(raw)
            if value.get('type')=='snapshot':states[name]=value
    async def connect(name):
        ticket=api('/api/ticket',dict(characterId=ids[name]),tokens[name])['ticket'];ws=await websockets.connect(BASE.replace('http','ws',1)+'/ws');sockets[name]=ws;seq[name]=0;sent[name]=asyncio.get_running_loop().time()
        await ws.send(json.dumps(dict(type='auth',ticket=ticket)));assert json.loads(await ws.recv())['version']==10;tasks.append(asyncio.create_task(watch(name,ws)))
    async def act(name,kind,target='',text=''):
        seq[name]+=1;sent[name]=asyncio.get_running_loop().time();await sockets[name].send(json.dumps(dict(type='action',seq=seq[name],kind=kind,target=target,text=text)))
    async def go(name,x,z):
        for _ in range(500):
            state=states[name];p=next(p for p in state['players'] if p['id']==ids[name]);dx,dz=x-p['x'],z-p['z'];seq[name]+=1;sent[name]=asyncio.get_running_loop().time()
            if (dx*dx+dz*dz)**.5<.25:
                await sockets[name].send(json.dumps(dict(type='input',seq=seq[name],x=0,z=0)));return
            await sockets[name].send(json.dumps(dict(type='input',seq=seq[name],x=dx,z=dz)));await wait(lambda:states[name]['tick']>state['tick'])
        raise AssertionError(f'navigation blocked: {name} -> {x},{z}: {states[name]["self"]["message"]}')
    def character(name):return states[name]['self']['character']
    def home(name,hid):return next(h for h in states[name]['community']['homes'] if h['id']==hid)
    async def heartbeat():
        while True:
            await asyncio.sleep(.5)
            for name,ws in list(sockets.items()):
                if ws.state.name=='OPEN' and asyncio.get_running_loop().time()-sent.get(name,0)>1:
                    seq[name]+=1;sent[name]=asyncio.get_running_loop().time()
                    await ws.send(json.dumps(dict(type='input',seq=seq[name],x=0,z=0)))
    tasks.append(asyncio.create_task(heartbeat()))
    try:
        for name in 'AB':
            suffix=secrets.token_hex(5);tokens[name]=api('/api/register',dict(username='h_'+suffix,password='M5-Test-Only-Password-123!'))['token']
            ids[name]=api('/api/characters',dict(slot=0,name='Wanderer'+name,classId='Tank'),tokens[name])['id'];await connect(name)
        await wait(lambda:len(states)==2)
        await go('A',0,24);await go('A',-8,24)
        for count in range(1,7):
            if count>1:await wait(lambda:character('A').get('inventory',{}).get('wood',0)==count-1 and states['A']['tick']>=gather_tick+102)
            await act('A','gather','wood');await wait(lambda:character('A')['inventory'].get('wood',0)==count);gather_tick=states['A']['tick']
        print('PROOF: gathered housing resources',flush=True)
        await go('A',-8,-3);await act('A','gather','ore');await wait(lambda:character('A')['inventory'].get('ore',0)==1)
        await go('A',-8,6);await act('A','craft','oak_chair');await wait(lambda:character('A')['inventory'].get('oak_chair',0)==1)
        await go('A',0,24);await go('A',30,24);await act('A','home_intro');await wait(lambda:states['A']['community']['housingIntro'])
        free=next(h for h in states['A']['community']['homes'] if not h['guildHall'] and h['owner']=='');hid=free['id'];x,z=free['x'],free['z']
        await act('A','home_claim',hid);await wait(lambda:home('A',hid)['owner']==ids['A'])
        print('PROOF: housing intro and ownership committed',flush=True)
        await act('A','guild_create','', 'Netzbund '+secrets.token_hex(3));await wait(lambda:states['A']['community']['guild'] is not None)
        await act('A','guild_invite',ids['B']);await wait(lambda:len(states['B']['community']['guildInvites'])==1)
        await act('B','guild_accept',states['B']['community']['guildInvites'][0]['id']);await wait(lambda:states['B']['community']['guild'] is not None)
        await act('A','home_visit',hid,ids['B']+'|allow')
        for name in 'AB':
            await go(name,0,24);await go(name,28,24);await go(name,28,z+9);await go(name,x-4,z+9);await go(name,x-4,z)
        await act('A','home_place',hid,f'oak_chair|{x-6}|{z-2}|1.2')
        await wait(lambda:len(home('B',hid)['furniture'] or [])==1)
        furniture=home('B',hid)['furniture'][0];assert furniture['yaw']==1.2 and 'oak_chair' not in character('A')['inventory']
        print('PROOF: both players see same furniture pose',flush=True)
        await sockets['A'].close();await wait(lambda:not any(p['id']==ids['A'] for p in states['B']['players']))
        await connect('A');await wait(lambda:len(home('A',hid)['furniture'] or [])==1);assert home('A',hid)['furniture'][0]['id']==furniture['id']
        await act('A','home_remove',hid,furniture['id']);await wait(lambda:character('A')['inventory'].get('oak_chair',0)==1)
        for name in 'AB':
            await go(name,x-4,z+9);await go(name,28,z+9);await go(name,28,24);await go(name,30,12)
        await act('A','market_list','','oak_chair|1|10');await wait(lambda:any(o['seller']==ids['A'] for o in states['B']['community']['auctions']))
        offer=next(o for o in states['B']['community']['auctions'] if o['seller']==ids['A']);await act('B','market_buy',offer['id']);await wait(lambda:character('B')['inventory'].get('oak_chair',0)==1)
        await wait(lambda:states['A']['community']['proceeds']==9);await act('A','market_claim');await wait(lambda:states['A']['self']['coins']==9)
        print('PASS: public account/craft/housing quest, two-client shared furniture, reconnect, guild and atomic auction journey')
    finally:
        for ws in sockets.values():await ws.close()
        for task in tasks:task.cancel()
asyncio.run(main())
