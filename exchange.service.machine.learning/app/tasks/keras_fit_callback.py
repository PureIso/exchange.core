"""Keras Callback class
"""
import tensorflow as tf

class KerasFitCallback(tf.keras.callbacks.Callback):
    """Keras callback to see the internal state and statistics of the model.
    https://keras.io/guides/writing_your_own_callbacks/

    Args:
        tf ([tf.keras.callbacks.Callback]): Abstract base class for keras callbacks
    """
    def __init__(self, training, epochs):
        """Called automatically every time the class is being used to create a new object.

        Args:
            training (PromiseProxy): Celery method that helps set task state
            https://www.distributedpython.com/2018/09/28/celery-task-states/
            epochs (int): The max epochs
        """
        self.training = training
        self.max_epoch = epochs
        super(KerasFitCallback, self).__init__()

    def on_epoch_end(self, epoch, logs=None):
        """Called at the end of an epoch during training.

        Args:
            epoch (int): an arbitrary cutoff, used to separate training into distinct phases.
            logs ([type], optional): [description]. Defaults to None.
        """
        current_training_status = {
            'state': 'EPOCH TESTING',
            'batch': 'N/A',
            'loss': logs['loss'],
            'epoch': "{0} of {1}".format(epoch, self.max_epoch),
            'status': 'Epoch Testing...'
        }
        self.training.update_state(state="PROGRESS", meta=current_training_status)
        print("End epoch {0} of training; loss: {1}".format(epoch, logs['loss']))
