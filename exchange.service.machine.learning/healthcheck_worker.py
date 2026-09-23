"""Container health probe for the Celery worker process."""
import os
from pathlib import Path


def is_worker_running(proc_root=Path("/proc")):
    """Return whether a Celery worker process is visible in procfs."""
    current_pid = str(os.getpid())
    for command_path in proc_root.glob("[0-9]*/cmdline"):
        if command_path.parent.name == current_pid:
            continue
        try:
            command = command_path.read_bytes()
        except OSError:
            # A process may exit between globbing procfs and reading it.
            continue
        if b"/opt/venv/bin/celery" in command and b"worker" in command:
            return True
    return False


if __name__ == "__main__":
    raise SystemExit(0 if is_worker_running() else 1)
