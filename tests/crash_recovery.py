"""Kill a disposable real server after an acknowledged SQL commit, then re-login.

Uses only the public API. Never points at a running deployment: owns both child
processes and binds a dedicated loopback port. Requires MMORPG_TEST_DATABASE.
"""
import asyncio
import json
import os
from pathlib import Path
import secrets
import subprocess
import urllib.error
import urllib.request

import websockets

BASE = "http://127.0.0.1:5082"
ROOT = Path(__file__).resolve().parent.parent


def api(path, body=None, token=""):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(BASE + path, headers=headers,
        data=None if body is None else json.dumps(body).encode())
    with urllib.request.urlopen(request, timeout=2) as response:
        return json.load(response)


async def snapshot(ws, predicate):
    async with asyncio.timeout(10):
        async for raw in ws:
            value = json.loads(raw)
            if value.get("type") == "snapshot" and predicate(value):
                return value
    raise AssertionError("Missing snapshot")


async def start(log):
    env = dict(os.environ, MMORPG_DATABASE=os.environ["MMORPG_TEST_DATABASE"],
               MMORPG_ALLOW_GUEST="false")
    process = subprocess.Popen([os.environ.get("DOTNET", "dotnet"),
        str(ROOT / "Server/bin/Release/net8.0/MMORPG.Server.dll"),
        "--urls", BASE], cwd=ROOT, env=env, stdout=log, stderr=log)
    try:
        async with asyncio.timeout(20):
            while True:
                if process.poll() is not None:
                    raise AssertionError("Disposable server exited at startup")
                try:
                    health = api("/health")
                    assert health["persistenceReady"] and not health["guestMode"]
                    return process
                except (urllib.error.URLError, TimeoutError):
                    await asyncio.sleep(.1)
    except BaseException:
        if process.poll() is None:
            process.kill()
        process.wait(timeout=10)
        raise


async def main():
    process = None
    ws = None
    log_path = Path(os.environ.get("CRASH_LOG", str(ROOT / "work/crash-server.log")))
    log_path.parent.mkdir(parents=True, exist_ok=True)
    with log_path.open("w") as log:
        try:
            process = await start(log)
            credentials = dict(username="crash_" + secrets.token_hex(5),
                               password="Crash-Proof-Test-Password-123!")
            token = api("/api/register", credentials)["token"]
            character = api("/api/characters", dict(slot=0, name="Chronistin",
                classId="Healer", appearance="neutral|1|2|1|2|1"), token)
            cid = character["id"]
            ticket = api("/api/ticket", dict(characterId=cid), token)["ticket"]
            unused = api("/api/ticket", dict(characterId=cid), token)["ticket"]
            ws = await websockets.connect(BASE.replace("http", "ws", 1) + "/ws")
            await ws.send(json.dumps(dict(type="auth", ticket=ticket)))
            assert json.loads(await ws.recv())["id"] == cid
            await snapshot(ws, lambda s: True)
            await ws.send(json.dumps(dict(type="action", seq=1, kind="pet", target="heal")))
            await snapshot(ws, lambda s: s["self"]["character"]["pet"]["active"])
            await ws.send(json.dumps(dict(type="action", seq=2, kind="slot", target="0|skill2")))
            await snapshot(ws, lambda s: s["self"]["character"]["skills"][0]["id"] == "skill2")
            await ws.send(json.dumps(dict(type="action", seq=3, kind="save_now")))
            await snapshot(ws, lambda s: s["self"]["message"] == "Charakterzustand dauerhaft bestätigt.")
            confirmed = api("/api/characters", token=token)[0]
            receipt = confirmed["lastCommit"]
            assert len(receipt) == 64 and api("/api/commits/" + receipt, token=token)["committed"]
            # SIGKILL, no WebSocket disconnect or hosted-service shutdown checkpoint.
            process.kill()
            process.wait(timeout=10)
            process = None
            await ws.close()
            ws = None
            process = await start(log)
            try:
                api("/api/characters", token=token)
                raise AssertionError("Old bearer session survived process restart")
            except urllib.error.HTTPError as error:
                assert error.code == 401
            token = api("/api/login", credentials)["token"]
            restored = api("/api/characters", token=token)[0]
            assert api("/api/commits/" + receipt, token=token)["committed"]
            assert restored == confirmed, "Persisted character document changed across SIGKILL"
            async with websockets.connect(BASE.replace("http", "ws", 1) + "/ws") as replay:
                await replay.send(json.dumps(dict(type="auth", ticket=unused)))
                try:
                    await replay.recv()
                    raise AssertionError("Pre-restart ticket accepted")
                except websockets.ConnectionClosed:
                    pass
            ticket = api("/api/ticket", dict(characterId=cid), token)["ticket"]
            ws = await websockets.connect(BASE.replace("http", "ws", 1) + "/ws")
            await ws.send(json.dumps(dict(type="auth", ticket=ticket)))
            assert json.loads(await ws.recv())["id"] == cid
            state = await snapshot(ws, lambda s: True)
            assert state["self"]["character"]["pet"]["mode"] == "heal"
            assert state["self"]["character"]["skills"][0]["id"] == "skill2"
            print("PASS: acknowledged whole character survives SIGKILL; old sessions/tickets rejected; fresh login reconnects")
        finally:
            if ws is not None:
                await ws.close()
            if process is not None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=10)


asyncio.run(main())
