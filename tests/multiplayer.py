"""Real transport regression: run against a started server, no mock or external host."""
import asyncio
import json
import math
import os
import websockets

URL = os.environ.get('TEST_WS_URL', 'ws://127.0.0.1:5080/ws')

async def snapshot(ws, predicate):
    async with asyncio.timeout(5):
        while True:
            msg = json.loads(await ws.recv())
            if msg['type'] == 'snapshot' and predicate(msg):
                return msg

async def main():
    async with websockets.connect(URL) as a, websockets.connect(URL) as b:
        wa, wb = json.loads(await a.recv()), json.loads(await b.recv())
        assert wa['type'] == wb['type'] == 'welcome'
        assert wa['version'] == 4 and wa['id'] != wb['id']
        aid, bid = wa['id'], wb['id']
        initial = await snapshot(b, lambda s: len(s['players']) == 2)
        origin = next(p for p in initial['players'] if p['id'] == aid)
        # Direction magnitude cannot buy speed, client coordinates are ignored.
        for seq in range(1, 11):
            await a.send(json.dumps(dict(type='input', seq=seq, x=100, z=100, position=9999)))
            await asyncio.sleep(.05)
        moved = await snapshot(b, lambda s: any(p['id'] == aid and p['x'] > origin['x'] + .5 for p in s['players']))
        player = next(p for p in moved['players'] if p['id'] == aid)
        distance = math.hypot(player['x'] - origin['x'], player['z'] - origin['z'])
        assert .5 < distance < 4, distance
        await asyncio.sleep(.5)  # Stale input must stop, even after last snapshot queued.
        stopped = await snapshot(b, lambda s: s['tick'] > moved['tick'] + 12)
        final = await snapshot(b, lambda s: s['tick'] > stopped['tick'] + 5)
        p1 = next(p for p in stopped['players'] if p['id'] == aid)
        p2 = next(p for p in final['players'] if p['id'] == aid)
        assert p1 == p2, (p1, p2)
        await a.send(json.dumps(dict(type='input', seq=1, x=-1, z=0)))
        replay = await snapshot(b, lambda s: s['tick'] > final['tick'] + 3)
        assert next(p for p in replay['players'] if p['id'] == aid) == p2
        # Fragmented text remains a single bounded input message.
        await a.send(['{"type":"input",', '"seq":11,"x":0,"z":0}'])
        await a.close()
        await snapshot(b, lambda s: len(s['players']) == 1 and s['players'][0]['id'] == bid)
    for payload in ['{', '{"type":"input","seq":1,"x":"NaN","z":0}', 'x' * 4097, b'binary']:
        async with websockets.connect(URL) as bad:
            assert json.loads(await bad.recv())['type'] == 'welcome'
            await bad.send(payload)
            try:
                async with asyncio.timeout(5):
                    while True:
                        await bad.recv()
            except websockets.ConnectionClosed:
                pass
    async with websockets.connect(URL) as fresh:
        assert json.loads(await fresh.recv())['type'] == 'welcome'
        await snapshot(fresh, lambda s: len(s['players']) == 1)
    print('PASS: two clients, authoritative speed, stale-input stop, disconnect, invalid/oversized input, reconnect')

async def gameplay_transport():
    async with websockets.connect(URL) as hero, websockets.connect(URL) as observer:
        identity = json.loads(await hero.recv())['id']
        observer_id = json.loads(await observer.recv())['id']
        seq = 0
        async def action(kind, target=''):
            nonlocal seq
            seq += 1
            await hero.send(json.dumps(dict(type='action', seq=seq, kind=kind, target=target, coins=999999, hp=999)))
        async def go(x,z):
            nonlocal seq
            for _ in range(220):
                state = await snapshot(hero, lambda s: True)
                p = next(p for p in state['players'] if p['id']==identity)
                dx,dz=x-p['x'],z-p['z']
                seq += 1
                if math.hypot(dx,dz)<.2:
                    await hero.send(json.dumps(dict(type='input',seq=seq,x=0,z=0)))
                    return state
                await hero.send(json.dumps(dict(type='input',seq=seq,x=dx,z=dz)))
            raise AssertionError('Unable to reach '+str((x,z)))
        await go(-3,7)
        await action('interact')
        accepted=await snapshot(hero,lambda s:s['self']['quest']=='active')
        assert accepted['self']['coins']==0
        await go(-6,-6)
        await action('target','drone-1')
        selection=await snapshot(hero,lambda s:any(p['id']==identity and p['target']=='drone-1' for p in s['players']))
        await snapshot(hero,lambda s:s['tick']>selection['tick']+3)
        await action('attack')
        hurt=await snapshot(hero,lambda s:any(m['id']=='drone-1' and m['hp']<m['maxHp'] for m in s['monsters']))
        seen=await snapshot(observer,lambda s:any(p['id']==identity and p['attackSerial']>0 for p in s['players']))
        assert any(m['id']=='drone-1' and m['hp']<m['maxHp'] for m in seen['monsters'])
        assert seen['self']['quest']=='available' and seen['self']['coins']==0
        assert all('coins' not in p for p in seen['players']), 'private progression leaked'
        print('PASS: real two-player quest/action/combat/HP replication, private state and reward spoof rejection')

async def run():
    await main()
    await gameplay_transport()

asyncio.run(run())
