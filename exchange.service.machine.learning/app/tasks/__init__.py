"""Task Worker init file"""
import os
import json
import datetime
import pandas as pd
from app.configuration import Configuration


def dateparse(datetime_str):
    """Method for datetime parsing in the following format: '%d/%m/%Y %H:%M:%S'

    Args:
        datetime_str (string): The datetime string

    Returns:
        string: The parsed datetime string
    """
    return datetime.datetime.strptime(datetime_str, '%d/%m/%Y %H:%M:%S')


def dateparse_extended(datetime_str):
    """Method for datetime parsing in the following format: '%m/%d/%Y %H:%M:%S'

    Args:
        datetime_str (string): The datetime string

    Returns:
        string: The parsed datetime string
    """
    return datetime.datetime.strptime(datetime_str, '%m/%d/%Y %H:%M:%S')


def normalize(value, min_value, max_value, new_min, new_max):
    """Normalize a value between new Min and new Max

    Args:
        value (string): The actual value
        min_value (string): The min range
        max_value (string): The max range
        new_min (string): The new min range
        new_max (string): The new max range

    Returns:
        float: The normalized value rounded to 8 decimal places
    """
    return round(float(new_min + (value - min_value) *
                       (new_max - new_min) / (max_value - min_value)), 8)


def get_training_dataset(indicator_file_name):
    """Read CSV file to data set array

    Args:
        csv_path (string): The csv file path

    Returns:
        array: The data set to train
    """
    csv_path = Configuration.get_file(indicator_file_name)
    dataset_train = []
    try:
        dataset_train = pd.read_csv(
            csv_path, parse_dates=True, index_col='DateTime', date_parser=dateparse)
    except TypeError:
        dataset_train = pd.read_csv(
            csv_path, parse_dates=True, index_col='DateTime', date_parser=dateparse_extended)
    return dataset_train


def load_normalized_data_from_json(indicator_file_name,
                                   min_close_value=None, max_close_value=None):
    """[summary]

    Args:
        indicator_file_name (string): [description]
        min_close_value (float): [description]
        max_close_value (float): [description]

    Returns:
        dict: dictionary result
    """
    epochs = 3550
    normalizer_path = Configuration.get_file_normalized_file(indicator_file_name)
    result = {'indicator_id': indicator_file_name,
              'min_close_value': min_close_value,
              'max_close_value': max_close_value,
              'epochs': epochs}
    try:
        with open(normalizer_path, 'r+') as json_file:
            data = json.load(json_file)
        if min_close_value is None or max_close_value is None:
            result['min_close_value'] = data['min_close_value']
            result['max_close_value'] = data['max_close_value']
            result['epochs'] = data['epochs']
            return result
        
        if min_close_value < data['min_close_value'] or \
                max_close_value > data['max_close_value']:
            save_normalized_data_to_json(
                normalizer_path, indicator_file_name, min_close_value, max_close_value, epochs)
            result['min_close_value'] = data['min_close_value']
            result['max_close_value'] = data['max_close_value']
            result['epochs'] = data['epochs']
            return result
    except IOError:
        save_normalized_data_to_json(
            normalizer_path, indicator_file_name, min_close_value, max_close_value, epochs)
    return result


def save_normalized_data_to_json(normalizer_path, indicator_file_name,
                                 min_close_value, max_close_value, epochs):
    """[summary]

    Args:
        normalizer_path ([type]): [description]
        indicator_file_name ([type]): [description]
        min_close_value ([type]): [description]
        max_close_value ([type]): [description]
        epochs ([type]): [description]
    """
    with open(normalizer_path, 'w') as outfile:
        result = {'indicator_id': indicator_file_name,
                  'min_close_value': min_close_value,
                  'max_close_value': max_close_value,
                  'epochs': epochs}
        json.dump(result, outfile)


def normalize_array(data_list, min_close_value, max_close_value):
    """Normalise list of values between 0 and 1

    Args:
        data_list (list of float): [description]
        min_close_value (float): [description]
        max_close_value (float): [description]

    Returns:
        list of float: [description]
    """
    data_length = len(data_list)
    normalized_data = []
    for index in range(data_length):
        value = data_list.values[index, 0]
        normalized_value = normalize(
            value, min_close_value, max_close_value, 0, 1)
        normalized_data.append([normalized_value])
    return normalized_data


def reverse_normalize_array(data_list, min_close_value, max_close_value):
    """reverse normalise list of values from 0 and 1 to original values

    Args:
        data_list (list of int): [description]
        min_close_value (float): [description]
        max_close_value (float): [description]

    Returns:
        list of float: [description]
    """
    data_length = len(data_list)
    normalized_data = []
    for index in range(data_length):
        normalized_value = normalize(
            data_list[index], 0, 1, min_close_value, max_close_value)
        normalized_data.append(normalized_value)
    return normalized_data
