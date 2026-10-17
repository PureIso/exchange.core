"""WSGI entry point used by the container's production HTTP server."""
from flask import Flask

from main import create_app


application: Flask = create_app()
