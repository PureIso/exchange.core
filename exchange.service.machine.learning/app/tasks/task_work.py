import os
import sys
import datetime
import json
import pandas as pd
import numpy as np
import tensorflow as tf
import tensorflowjs as tfjs
from . import dateparse, dateparse_extended, normalize, config
from celery import Celery
from tensorflow import keras
from keras.models import Sequential, load_model
from keras.layers import Dense, Dropout, LSTM
from sklearn.model_selection import train_test_split
from flask import Response
from pathlib import Path
from app.exchange import Exchange
exchange = Exchange()
celery = exchange.celery

def get_training_dataset(csv_path):
    dataset_train = []
    try:
        dataset_train = pd.read_csv(
            csv_path, parse_dates=True, index_col='DateTime', date_parser=dateparse)
    except Exception:
        dataset_train = pd.read_csv(
            csv_path, parse_dates=True, index_col='DateTime', date_parser=dateparse_extended)
    return dataset_train

@celery.task(bind=True)
def training(self, indicator_file, save, predict, previous_prices):
    print("MI Training: {0} {1} {2} {3}".format(indicator_file,save,predict,previous_prices))
    if indicator_file != None and save != None:
        # Get the input options
        csv_path = ''
        normalizer_path = ''
        modelh5_path = ''
        kerasModelDirectory = ''
        epochs = 270

        self.update_state(state="STARTING", meta=exchange.current_training_status)
        # Setting the variables
        epochs = 3550
        csv_path = config.getFile(indicator_file)
        normalizer_path = config.getFileNormalizedFile(indicator_file)
        modelh5_path = config.getFileModelFile(indicator_file)
        kerasModelDirectory = config.getDirectory(indicator_file)

        # import dataset using pandas
        dataset_train = get_training_dataset(csv_path)
        # get specific columns - training feature set
        feature_set_dataframe = dataset_train[['Close', 'RSI14']]
        feature_set_dataframe_length = len(feature_set_dataframe)
        if previous_prices == [] or previous_prices == None:
            print("Last 5 Data")
            previous = dataset_train[['Close']].values[-5:] 
            print(previous)
        # get min and max values
        minValue = round(float(min(feature_set_dataframe['Close'])),8)
        maxValue = round(float(max(feature_set_dataframe['Close'])),8)

        print("Saving Normalized files.")
        if save == True:
            try:
                with open(normalizer_path,'r+') as json_file:
                    data = json.load(json_file)
                    if minValue < data['minValue'] or maxValue > data['maxValue']:
                        data = {'indicator_id': indicator_file,
                                'minValue': minValue,
                                'maxValue': maxValue, 'epochs': epochs}
                        json.dump(data, normalizer_path)
                        # json_file.write(str(data))
                    minValue = data['minValue']
                    maxValue = data['maxValue']
                    epochs = data['epochs']
            except Exception:
                with open(normalizer_path, 'w') as outfile:
                    data = {'indicator_id': indicator_file,
                            'minValue': minValue,
                            'maxValue': maxValue, 'epochs': epochs}
                    json.dump(data, outfile)
                    # outfile.write(str(data))

        print("Normalizing values.")
        # normalize the values
        # Makes the gradient low which will means easier learning
        normalized_feature_set_array = []
        for index in range(feature_set_dataframe_length):
            # get the value at index and column 0
            value = feature_set_dataframe.values[index, 0]
            normalized_value = normalize(value, minValue, maxValue, 0, 1)
            normalized_feature_set_array.append(
                [normalized_value])

        # self.update_state(state="PROGRESS", meta={'progress': 50})
        # create the data structure for out training data
        # 5 days worth of data per feature
        timesteps = 5
        x_feature_high_price = []
        y_label_simple_moving_average = []

        print("Normalizing values.")
        # validate feature set length
        if feature_set_dataframe_length < timesteps:
            response = json.dumps({"message": 'Invalid timesteps: {0}'.format(timesteps)})
            return response
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
            response = json.dumps({"message": 'Input length is invalid'})
            return response
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
        history = ''

        # initialize the Recurrent Neural Network
        # Solving pattern with sequence of data
        regressor = keras.models.Sequential()
        try:
            if predict == True:
                print('Loading Regressor Model: {0}'.format(modelh5_path))
                regressor = keras.models.load_model(modelh5_path)
                regressor.summary()
                accuracy = regressor.evaluate(test_set_input, test_set_output)
                print('Restored model, accuracy: {:5.2f}%'.format(100*accuracy))
            else:
                self.update_state(state="PROGRESS", meta={'progress': 70})
                print('New Regressor Model: {0}'.format(modelh5_path))
                neurons = 50
                batch_size = 32
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
                history = regressor.fit(train_set_input, train_set_output,
                                        batch_size=batch_size, epochs=epochs, steps_per_epoch=5, validation_data=(test_set_input, test_set_output),  verbose=0, callbacks=[KerasFitCallback()])
                if save == True:
                    print("Saving Model")
                    # save the model for javascript format readable
                    tfjs.converters.save_keras_model(regressor, kerasModelDirectory)
                    # save the model for python format readable
                    regressor.save(modelh5_path)
        except Exception:
            print('Initial Regressor Model: {0}'.format(modelh5_path))
            self.update_state(state="PROGRESS", meta={'progress': 70})
            neurons = 50
            batch_size = 32
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
            history = regressor.fit(train_set_input, train_set_output,
                                    batch_size=batch_size, epochs=epochs, steps_per_epoch=5, validation_data=(test_set_input, test_set_output),  verbose=0, callbacks=[KerasFitCallback()])
            if save == True:
                print("Saving Model")
                # save the model for javascript format readable
                tfjs.converters.save_keras_model(regressor, kerasModelDirectory)
                # save the model for python format readable
                regressor.save(modelh5_path)
        
        if previous_prices == []:
            print('Test Prediction:')
            print(test_set_input)
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
            
            if(predict == True):
                training_result = json.dumps({
                    'result': str(json.dumps({
                        'ClosePrice': x_label_sma_normalized,
                        'ClosePricePredict': y_label_sma_normalized}))})
            else:
                training_result = json.dumps({'current': 100,
                                'total': 100,
                                'status': 'Task completed!',
                                'details': str(json.dumps(exchange.current_training_status)),
                                'summary': str(regressor.summary()),
                                'history': str(history),
                                'result': str(json.dumps({
                                    'ClosePrice': x_label_sma_normalized,
                                    'ClosePricePredict': y_label_sma_normalized}))})
            # exchange.get_mongo_database().training_result.insert_one(training_result)
            print(training_result)
            response = training_result
        else:
            print('Prediction:')
            normalized_previous_price_set_array = []
            final_price_array = []
            for previous_price in previous_prices:
                normalized_value = normalize(round(float(previous_price),8), minValue, maxValue, 0, 1)
                normalized_previous_price_set_array.append([normalized_value])

            print(normalized_previous_price_set_array)
            input_value_flatten = np.array(normalized_previous_price_set_array)
            final_price_array.append(input_value_flatten.flatten())
            print(final_price_array)

            predicted_price_input_shape = (len(final_price_array), timesteps, 1)
            print(predicted_price_input_shape)
            previous_prices = np.reshape(final_price_array, predicted_price_input_shape)

            print(previous_prices)
            predicted_price = regressor.predict(previous_prices)
            y_label_sma = np.array(predicted_price)
            final_output_length = len(y_label_sma)
            y_label_sma_normalized = []
            for index in range(final_output_length):
                y_label_sma_normalized.append(normalize(y_label_sma[index], 0, 1, minValue, maxValue))
            
            training_result = json.dumps({'result': str(json.dumps({'ClosePricePredict': y_label_sma_normalized}))})
            # exchange.get_mongo_database().training_result.insert_one(training_result)
            print(training_result)
            response = training_result
    else:
        response = json.dumps({"message": 'Invalid input'})
    return response


class KerasFitCallback(tf.keras.callbacks.Callback):
    def on_epoch_end(self, epoch, logs=None):
        current_training_status = {
            'state': 'EPOCH TESTING',
            'batch': 'N/A',
            'loss': logs,
            'epoch': epoch,
            'status': 'Epoch Testing...'
        }
        training.update_state(state="PROGRESS", meta=current_training_status)
        print('The average loss for epoch {}.'.format(epoch))
