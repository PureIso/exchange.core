"""Task Work File"""
import json
import numpy as np
import tensorflowjs as tfjs
from tensorflow import keras
from sklearn.model_selection import train_test_split
from app.tasks.keras_fit_callback import KerasFitCallback
from app.configuration import Configuration
from . import get_training_dataset, normalize
from . import load_normalized_data_from_json
from . import normalize_array, reverse_normalize_array

configuration = Configuration()
celery = configuration.celery


@celery.task(bind=True)
def training(self, indicator_file_name):
    """[summary]

    Args:
        indicator_file_name ([type]): [description]

    Returns:
        [type]: [description]
    """
    self.update_state(state="STARTING",
                      meta=configuration.current_training_status)
    # import dataset using pandas
    training_dataset = get_training_dataset(indicator_file_name)
    # get min and max values
    min_close_value = round(float(min(training_dataset['Close'])), 8)
    max_close_value = round(float(max(training_dataset['Close'])), 8)

    data = load_normalized_data_from_json(indicator_file_name,
                                          min_close_value,
                                          max_close_value)

    # normalize the values
    # Makes the gradient low which will means easier learning
    normalized_closed_training_dataset = normalize_array(training_dataset[['Close']],
                                                         data['min_close_value'],
                                                         data['max_close_value'])

    # create the data structure for out training data
    # 5 days worth of data per feature
    # Example:
    # x_close_prices_input: [
    # array([0.94815855, 0.94440543, 0.96821957, 0.95111262, 0.96068912]),
    # array([0.94440543, 0.96821957, 0.95111262, 0.96068912, 0.95193588]),
    # array([0.96821957, 0.95111262, 0.96068912, 0.95193588, 0.97259014]),
    # array([0.95111262, 0.96068912, 0.95193588, 0.97259014, 0.95405458]),
    # array([0.96068912, 0.95193588, 0.97259014, 0.95405458, 0.97251749]),
    # array([0.95193588, 0.97259014, 0.95405458, 0.97251749, 1.        ]),
    # array([0.97259014, 0.95405458, 0.97251749, 1.        , 0.98588344]),
    # array([0.95405458, 0.97251749, 1.        , 0.98588344, 0.97688806]),

    # y_close_price_output: [
    # array([0.95193588]),
    # array([0.97259014]),
    # array([0.95405458]),
    # array([0.97251749]),
    # array([1.]),
    # array([0.98588344]),
    x_close_prices_input = []
    y_close_price_output = []
    for index in range(5, len(normalized_closed_training_dataset)):
        if len(normalized_closed_training_dataset) < (index + 1):
            break
        # generate the subsequent value as output
        x_close_prices_input.append(
            np.array(normalized_closed_training_dataset[index - 5:index]).flatten())
        y_close_price_output.append(
            np.array(normalized_closed_training_dataset[index:index+1]).flatten())

    # 'train_set_input': array([[[0.94815855],[0.94440543],[0.96821957],
    #                            [0.95111262],[0.96068912]],.)
    # 'train_set_output': array([[0.95193588],...)
    reshaped_data_dict = get_reshaped_input_and_output_data(x_close_prices_input,
                                                            y_close_price_output,
                                                            5)
    regressor_result = initialise_model_lstm(indicator_file_name,
                                             5,
                                             reshaped_data_dict['test_set_input'],
                                             reshaped_data_dict['test_set_output'],
                                             reshaped_data_dict['train_set_input'],
                                             reshaped_data_dict['train_set_output'],
                                             data['epochs'])

    x_real_close_price_normalized = reverse_normalize_array(
        np.array(reshaped_data_dict['test_set_output']),
        data['min_close_value'],
        data['max_close_value'])
    y_predicted_price_normalized = reverse_normalize_array(
        np.array(regressor_result['predicted_price']),
        data['min_close_value'],
        data['max_close_value'])

    training_result = json.dumps({'current_progress': 100,
                                  'total_progress': 100,
                                  'status': 'Task completed!',
                                  'close_price': x_real_close_price_normalized,
                                  'close_price_prediction': y_predicted_price_normalized})
    return training_result


@celery.task(bind=True)
def prediction(self, indicator_file_name):
    """[summary]

    Args:
        indicator_file_name ([type]): [description]

    Returns:
        [type]: [description]
    """
    self.update_state(state="STARTING",
                      meta=configuration.current_training_status)
    # import dataset using pandas
    training_dataset = get_training_dataset(indicator_file_name)
    # get min and max values
    data = load_normalized_data_from_json(indicator_file_name)

    # normalize the values
    # Makes the gradient low which will means easier learning
    x_close_prices_input = training_dataset[['Close']].values[-5:]
    data_length = len(x_close_prices_input)
    normalized_closed_training_dataset = []
    for index in range(data_length):
        value = x_close_prices_input[index, 0]
        normalized_value = normalize(
            value, data['min_close_value'], data['max_close_value'], 0, 1)
        normalized_closed_training_dataset.append([normalized_value])

    # create the data structure for out training data
    # 5 days worth of data per feature
    x_normalized_close_prices_input = []
    x_normalized_close_prices_input.append(
        np.array(normalized_closed_training_dataset).flatten())
    # define training input and output shapes
    x_normalized_close_prices_input_shape = (
        len(x_normalized_close_prices_input), 5, 1)
    # reshape training input and outputs
    reshaped_data = np.reshape(
        x_normalized_close_prices_input, x_normalized_close_prices_input_shape)
    regressor_result = lstm_prediction(indicator_file_name, reshaped_data)
    predicted_price_normalized = reverse_normalize_array(
        np.array(regressor_result['predicted_price']),
        data['min_close_value'],
        data['max_close_value'])

    training_result = json.dumps({'current_progress': 100,
                                  'total_progress': 100,
                                  'status': 'Task completed!',
                                  'close_price': np.array(x_close_prices_input.flatten()).tolist(),
                                  'close_price_prediction': predicted_price_normalized})
    return training_result


def get_reshaped_input_and_output_data(x_close_prices_input, y_close_price_output, timesteps):
    """[summary]

    Args:
        x_close_prices_input ([type]): [description]
        y_close_price_output ([type]): [description]
        timesteps ([type]): [description]

    Returns:
        [type]: [description]
    """
    # split training and test
    # output would now be an array
    train_set_input, test_set_input = train_test_split(
        x_close_prices_input,
        test_size=0.25,
        shuffle=False)
    train_set_output, test_set_output = train_test_split(
        y_close_price_output,
        test_size=0.25,
        shuffle=False)

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
    result = {'train_set_input': train_set_input,
              'train_set_output': train_set_output,
              'test_set_input': test_set_input,
              'test_set_output': test_set_output}
    return result


def initialise_model_lstm(indicator_file_name,
                          timesteps, test_set_input, test_set_output,
                          train_set_input, train_set_output, epochs):
    """[summary]

    Args:
        indicator_file_name ([type]): [description]
        timesteps ([type]): [description]
        test_set_input ([type]): [description]
        test_set_output ([type]): [description]
        train_set_input ([type]): [description]
        train_set_output ([type]): [description]
        epochs ([type]): [description]

    Returns:
        [type]: [description]
    """
    keras_model_directory = Configuration.get_directory(indicator_file_name)
    modelh5_path = Configuration.get_file_model_file(indicator_file_name)
    # define the RNN input shape
    lstm_input_shape = (timesteps, 1)
    # initialize the Recurrent Neural Network
    # Solving pattern with sequence of data
    regressor = keras.models.Sequential()
    training.update_state(state="PROGRESS", meta={'progress': 70})
    neurons = 50
    batch_size = 32

    # Long Short Term Memory model -
    # supervised Deep Neural Network that is very good at doing time-series prediction.
    # adding the first LSTM Layer and some Dropout regularisation
    #
    # Dropout: Makes sure that network can never rely on any given activation
    # to be present because at any moment they could become squashed i.e. value = 0
    # forced to learn a redunant representation for everything
    #
    # return sequences: We want output after every layer in which will be
    # passed to the next layer
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
    regressor.compile(optimizer='adam', loss='mean_absolute_error')
    regressor.summary()
    accuracy = regressor.evaluate(test_set_input, test_set_output)
    print('Restored model, accuracy: {:5.2f}%'.format(100*accuracy))

    # fitting the RNN to the training set
    # giving input in batch sizes of 5
    # loss should decrease on each an every epochs
    history = regressor.fit(train_set_input, train_set_output,
                            batch_size=batch_size,
                            epochs=epochs,
                            steps_per_epoch=5,
                            validation_data=(test_set_input, test_set_output),
                            verbose=0,
                            callbacks=[KerasFitCallback(training, epochs)])

    # save the model for javascript format readable
    tfjs.converters.save_keras_model(
        regressor, keras_model_directory)
    # save the model for python format readable
    regressor.save(modelh5_path)
    predicted_price = regressor.predict(test_set_input)
    result = {'summary': str(regressor.summary()),
              'history': str(history),
              'predicted_price': predicted_price}
    return result


def lstm_prediction(indicator_file_name, data_input):
    modelh5_path = Configuration.get_file_model_file(indicator_file_name)
    # initialize the Recurrent Neural Network
    # Solving pattern with sequence of data
    regressor = keras.models.load_model(modelh5_path)
    predicted_price = regressor.predict(data_input)
    result = {'summary': str(regressor.summary()),
              'predicted_price': predicted_price}
    return result
