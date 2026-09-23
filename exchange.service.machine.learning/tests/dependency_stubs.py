"""Small dependency doubles used before importing the legacy application."""
import logging
import sys
import types
from unittest.mock import Mock


class _NullGraylogHandler(logging.NullHandler):
    def __init__(self, *args, **kwargs):
        super().__init__()


def install_import_stubs():
    """Avoid TensorFlow initialization and external Graylog traffic in unit tests."""
    tensorflow = types.ModuleType("tensorflow")
    tensorflow.keras = types.SimpleNamespace(
        callbacks=types.SimpleNamespace(Callback=object),
        models=types.SimpleNamespace(load_model=Mock()))
    graypy = types.ModuleType("graypy")
    graypy.GELFUDPHandler = _NullGraylogHandler

    sys.modules.setdefault("tensorflow", tensorflow)
    sys.modules.setdefault("graypy", graypy)
