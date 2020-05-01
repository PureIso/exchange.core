import celery
from . import dateparse, normalize, config

import os
import sys
import json
import pandas as pd
import numpy as np
from tensorflow import keras
from keras.models import Sequential, load_model
from keras.layers import Dense, Dropout, LSTM
from sklearn.model_selection import train_test_split
from flask_restful import Resource, reqparse
from flask import Flask, request, url_for, json
#import tensorflowjs as tfjs
#import tensorflow.compat.v1 as tf
# tf.disable_v2_behavior()
#import tensorflow as tf
parser = reqparse.RequestParser()


@celery.task
def some_long_task(hourly, save):
    # Validate directories
    # if not os.path.exists(hourly_directory):
    #     print("Validate Directory")
    #     os.makedirs(hourly_directory)
    # if not os.path.exists(daily_directory):
    #     print("Validate Directory")
    #     os.makedirs(daily_directory)

    # Get the input options
    csv_path = ''
    normalizer_path = ''
    modelh5_path = ''
    directory = ''
    epochs = 270

    print("Started")
    # Setting the variables
    if(hourly == False):
        epochs = 3550
        #directory = daily_directory
        csv_path = config.getDailyCSVFILE()
        normalizer_path = config.getDailyNormalisJSONFILE()
        modelh5_path = config.getHourlyModelH5FILE()
    else:
        epochs = 270
        csv_path = config.getHourlyCSVFILE()
        normalizer_path = config.getHourlyNormalisJSONFILE()
        modelh5_path = config.getHourlyModelH5FILE()

    # import dataset using pandas
    dataset_train = pd.read_csv(
        csv_path, parse_dates=True, index_col='DateTime', date_parser=dateparse)
    # get specific columns - training feature set
    feature_set_dataframe = dataset_train[['Close', 'RSI14']]
    feature_set_dataframe_length = len(feature_set_dataframe)
    # get min and max values
    minValue = float(min(feature_set_dataframe['Close']))
    maxValue = float(max(feature_set_dataframe['Close']))
    if save:
        try:
            with open(normalizer_path) as json_file:
                data = json.load(json_file)
                if minValue < data['minValue'] or maxValue > data['maxValue']:
                    data = {'minValue': minValue,
                            'maxValue': maxValue, 'epochs': epochs}
                    with open(normalizer_path, 'w') as outfile:
                        json.dump(data, outfile)
                minValue = data['minValue']
                maxValue = data['maxValue']
                epochs = data['epochs']
        except Exception:
            with open(normalizer_path, 'w') as outfile:
                data = {'minValue': minValue,
                        'maxValue': maxValue, 'epochs': epochs}
                json.dump(data, outfile)

    # normalize the values
    # Makes the gradient low which will means easier learning
    normalized_feature_set_array = []
    for index in range(feature_set_dataframe_length):
        # get the value at index and column 0
        value = feature_set_dataframe.values[index, 0]
        normalized_value = normalize(value, minValue, maxValue, 0, 1)
        normalized_feature_set_array.append(
            [normalized_value])

    # create the data structure for out training data
    # 5 days worth of data per feature
    timesteps = 5
    x_feature_high_price = []
    y_label_simple_moving_average = []

    # validate feature set length
    if feature_set_dataframe_length < timesteps:
        return
    for index in range(timesteps, feature_set_dataframe_length):
        start_row_index = index - timesteps
        end_row_index = index + 1
        if feature_set_dataframe_length < end_row_index:
            break
        # generate timesteps of 5 input
        input_value = normalized_feature_set_array[start_row_index:index]
        input_value_flatten = np.array(input_value)
        # generate the subsequent value as output
        output_value = normalized_feature_set_array[index:end_row_index]
        x_feature_high_price.append(input_value_flatten.flatten())
        y_label_simple_moving_average.append(output_value[0][0])

    if len(x_feature_high_price) <= 0:
        return
    # split training and test
    # output would now be an array
    train_set_input, test_set_input = train_test_split(
        x_feature_high_price, test_size=0.25, shuffle=False)
    train_set_output, test_set_output = train_test_split(
        y_label_simple_moving_average, test_size=0.25, shuffle=False)

    # define training input and output shapes
    train_input_shape = (len(train_set_input), timesteps, 1)
    train_output_shape = (len(train_set_output), 1)
    # define test input and output shapes
    test_input_shape = (len(test_set_input), timesteps, 1)
    test_output_shape = (len(test_set_output), 1)
    # reshape training input and outputs
    train_set_input = np.reshape(train_set_input, train_input_shape)
    train_set_output = np.reshape(train_set_output, train_output_shape)
    # reshape test input and outputs
    test_set_input = np.reshape(test_set_input, test_input_shape)
    test_set_output = np.reshape(test_set_output, test_output_shape)
    # define the RNN input shape
    lstm_input_shape = (timesteps, 1)
    # history = ''

    # initialize the Recurrent Neural Network
    # Solving pattern with sequence of data
    regressor = keras.models.Sequential()
    try:
        regressor = keras.models.load_model(modelh5_path)
        regressor.summary()
        accuracy = regressor.evaluate(test_set_input, test_set_output)
        print('Restored model, accuracy: {:5.2f}%'.format(100*accuracy))
    except Exception:
        neurons = 50
        # batch_size = 32
        # Long Short Term Memory model - supervised Deep Neural Network that is very good at doing time-series prediction.
        # adding the first LSTM Layer and some Dropout regularisation
        #
        # Dropout: Makes sure that network can never rely on any given activation to be present because at any moment they could become squashed i.e. value = 0
        # forced to learn a redunant representation for everything
        #
        # return sequences: We want output after every layer in which will be passed to the next layer
        regressor.add(keras.layers.LSTM(units=neurons, return_sequences=True,
                                        input_shape=lstm_input_shape))
        regressor.add(keras.layers.Dropout(0.2))

        # adding a second LSTM Layer and some Dropout regularisation
        regressor.add(keras.layers.LSTM(
            units=neurons, return_sequences=True))
        regressor.add(keras.layers.Dropout(0.2))

        # adding a third LSTM Layer and some Dropout regularisation
        regressor.add(keras.layers.LSTM(
            units=neurons, return_sequences=True))
        regressor.add(keras.layers.Dropout(0.2))

        # adding a fourth LSTM Layer and some Dropout regularisation
        regressor.add(keras.layers.LSTM(units=neurons))
        regressor.add(keras.layers.Dropout(0.2))

        # adding the output layer
        # Dense format output layer
        regressor.add(keras.layers.Dense(units=1))  # prediction

        # compile network
        # find the global minimal point
        regressor.compile(optimizer='adam',
                          loss='mean_absolute_error')
        regressor.summary()
        accuracy = regressor.evaluate(test_set_input, test_set_output)
        print('Restored model, accuracy: {:5.2f}%'.format(100*accuracy))

        # fitting the RNN to the training set
        # giving input in batch sizes of 5
        # loss should decrease on each an every epochs
        # history = regressor.fit(train_set_input, train_set_output,
        #                         batch_size=batch_size, epochs=epochs, validation_data=(test_set_input, test_set_output))
        if save:
            # save the model for javascript format readable
            # tfjs.converters.save_keras_model(
            #    regressor, "app/static/hourly_data")
            # save the model for python format readable
            regressor.save(modelh5_path)

    predicted_price = regressor.predict(test_set_input)

    y_label_sma = np.array(predicted_price)
    x_label_sma = np.array(test_set_output)

    final_output_length = len(x_label_sma)
    y_label_sma_normalized = []
    x_label_sma_normalized = []
    for index in range(final_output_length):
        y_label_sma_normalized.append(
            normalize(y_label_sma[index], 0, 1, minValue, maxValue))
        x_label_sma_normalized.append(
            normalize(x_label_sma[index], 0, 1, minValue, maxValue))

    # create a new display dataframe
    df = pd.DataFrame({
        'ClosePrice': x_label_sma_normalized,
        'ClosePricePredict': y_label_sma_normalized})
    print("Completed")
    return df

    # plt.plot(df['ClosePrice'],
    #         color='red', label='Close Price', alpha=0.8)
    # plt.plot(df['ClosePricePredict'],
    #         color='red', label='Close Price Predict', alpha=0.2)
    # beautify the x-labels
    # plt.gcf().autofmt_xdate()
   # plt.legend()
    # plt.show()

   # if(history != ''):
    # plt.plot(history.history['loss'])
    # plt.show()


class RecurrentNeuralNetwork(Resource):
    def get(self):
        return {'RNN': 'GET Page'}

    def post(self):
        parser.add_argument("hourly")
        parser.add_argument("save")
        parser.add_argument("spec")
        args = parser.parse_args()
        hourly = args['hourly']
        save = args['save']
        some_long_task.delay(hourly, save)
        return {'result:': "Processing"}
