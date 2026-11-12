"""Authenticated API/WebSocket ownership, reconnect and database roundtrip."""
import asyncio,json,os,urllib.request,urllib.error,secrets
import websockets
BASE=os.environ.get('TEST_AUTH_URL','http://127.0.0.1:5081')
def request(path,body=None,token=''):
    headers={'Content-Type':'application/json'}
    if token:headers['Authorization']='Bearer '+token
    req=urllib.request.Request(BASE+path,data=json.dumps(body).encode() if body is not None else None,headers=headers)
    with urllib.request.urlopen(req,timeout=10) as response:return json.load(response)
async def snap(ws,predicate):
    async with asyncio.timeout(8):
        while True:
            s=json.loads(await ws.recv())
            if s.get('type')=='snapshot' and predicate(s):return s
async def main():
    suffix=secrets.token_hex(5)
    login=request('/api/register',dict(username='m3_'+suffix,password='Test-Only-Password-123!'))
    token=login['token']
    a=request('/api/characters',dict(slot=0,name='Chronist',classId='Dark Mage'),token)
    for slot,cls in [(1,'Healer'),(2,'Tank')]:request('/api/characters',dict(slot=slot,name='Testheld',classId=cls),token)
    assert len(request('/api/characters',token=token))==3
    stranger=request('/api/register',dict(username='other_'+suffix,password='Test-Only-Password-123!'))['token']
    try:request('/api/ticket',dict(characterId=a['id']),stranger);raise AssertionError('foreign character ticket')
    except urllib.error.HTTPError as e:assert e.code==400
    ticket=request('/api/ticket',dict(characterId=a['id']),token)['ticket']
    uri=BASE.replace('http','ws',1)+'/ws'
    async with websockets.connect(uri) as ws:
        await ws.send(json.dumps(dict(type='auth',ticket=ticket)))
        welcome=json.loads(await ws.recv());assert welcome['version']==3 and welcome['id']==a['id']
        s=await snap(ws,lambda s:True)
        assert s['self']['character']['classId']=='Dark Mage'
        await ws.send(json.dumps(dict(type='action',seq=1,kind='pet',target='heal')))
        await snap(ws,lambda s:s['self']['character']['pet']['active'])
        async with websockets.connect(uri) as replay:
            await replay.send(json.dumps(dict(type='auth',ticket=ticket)))
            try:await replay.recv();raise AssertionError('ticket replay')
            except websockets.ConnectionClosed:pass
    # Wait only for the observed disconnect/save, rather than arbitrary startup sleeps.
    for attempt in range(40):
        listed=request('/api/characters',token=token)
        if listed[0]['state']['pet']['active']:break
        await asyncio.sleep(.05)
    assert listed[0]['state']['pet']['mode']=='heal'
    new_login=request('/api/login',dict(username='m3_'+suffix,password='Test-Only-Password-123!'))
    assert request('/api/characters',token=new_login['token'])[0]['id']==a['id']
    ticket2=request('/api/ticket',dict(characterId=a['id']),new_login['token'])['ticket']
    async with websockets.connect(uri) as ws:
        await ws.send(json.dumps(dict(type='auth',ticket=ticket2)))
        assert json.loads(await ws.recv())['id']==a['id']
        s=await snap(ws,lambda s:True)
        assert s['self']['character']['pet']['mode']=='heal'
    print('PASS: accounts, three slots, ownership, one-use auth ticket, durable state and re-login/reconnect')
asyncio.run(main())
