from subprocess import Popen, CREATE_NEW_CONSOLE

Popen(["py", "serve_web.py"], creationflags=CREATE_NEW_CONSOLE)
