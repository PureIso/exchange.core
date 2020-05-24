import os
import pandas as pd
from app.config import Config
config = Config()


def dateparse(datetime_str):
    return pd.datetime.strptime(datetime_str, '%d/%m/%Y %H:%M:%S')


def normalize(value, minValue, maxValue, newMin, newMax):
    return round(float(newMin + (value - minValue) *
                       (newMax - newMin) / (maxValue - minValue)), 8)
