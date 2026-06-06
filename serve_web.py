import os
import subprocess
import time

PROCESS_NAME = "DCSBiosTRC.exe"
POLL_INTERVAL = 1  # seconds
START_TIME = time.time()
TIMEOUT_SECONDS = 5

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
UI_DIR = os.path.join(SCRIPT_DIR, "ui")


def is_process_running(name):
    result = subprocess.run(
        ["tasklist", "/FI", f"IMAGENAME eq {name}"],
        capture_output=True,
        text=True,
    )
    return name.lower() in result.stdout.lower()


def timeoutElapsed():
    return time.time() > TIMEOUT_SECONDS + START_TIME


def wait_for_process(name, poll_interval=POLL_INTERVAL):
    print(f"Waiting for {name} to start...", flush=True)
    dots = 0
    while not is_process_running(name) and not timeoutElapsed():
        dots = (dots % 3) + 1
        print(f"\r  {'.' * dots}   ", end="", flush=True)
        time.sleep(poll_interval)
    print(f"\r{name} is running.        ", flush=True)


def watch_process(name, poll_interval=POLL_INTERVAL):
    while is_process_running(name):
        time.sleep(poll_interval)


if __name__ == "__main__":
    wait_for_process(PROCESS_NAME)
    print("Starting npm run dev...", flush=True)

    npm = subprocess.Popen(
        ["npm", "run", "dev"],
        cwd=UI_DIR,
        shell=True,
    )

    print(f"npm dev server started (pid {npm.pid}). Watching for {PROCESS_NAME} to close...", flush=True)

    watch_process(PROCESS_NAME)

    print(f"{PROCESS_NAME} closed. Shutting down npm dev server...", flush=True)
    subprocess.run(["taskkill", "/T", "/F", "/PID", str(npm.pid)], capture_output=True)
    npm.wait()
    print("Done.", flush=True)
