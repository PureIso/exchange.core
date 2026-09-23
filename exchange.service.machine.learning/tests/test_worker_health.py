import tempfile
import unittest
from pathlib import Path

from healthcheck_worker import is_worker_running


class WorkerHealthTests(unittest.TestCase):
    def test_detects_a_celery_worker_process(self):
        with tempfile.TemporaryDirectory() as directory:
            command_path = Path(directory) / "42" / "cmdline"
            command_path.parent.mkdir()
            command_path.write_bytes(
                b"/opt/venv/bin/python\0/opt/venv/bin/celery\0-A\0app\0worker\0")

            self.assertTrue(is_worker_running(Path(directory)))

    def test_rejects_an_unrelated_python_process(self):
        with tempfile.TemporaryDirectory() as directory:
            command_path = Path(directory) / "42" / "cmdline"
            command_path.parent.mkdir()
            command_path.write_bytes(b"/opt/venv/bin/python\0main.py\0")

            self.assertFalse(is_worker_running(Path(directory)))


if __name__ == "__main__":
    unittest.main()
