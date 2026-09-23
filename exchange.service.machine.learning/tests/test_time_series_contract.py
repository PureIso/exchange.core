"""Characterize deterministic preprocessing and the model-loading boundary."""
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

import numpy as np
import pandas as pd

from dependency_stubs import install_import_stubs

# Model execution is outside this deterministic suite. These doubles exercise
# the model-loading seam without initializing TensorFlow, a GPU, or Graylog.
install_import_stubs()

from app.tasks import (
    get_next_datetime,
    load_normalized_data_from_json,
    normalize,
    normalize_array,
    reverse_normalize_array,
)
from app.tasks.task_work import (
    convert_to_chart,
    get_reshaped_input_and_output_data,
    lstm_prediction,
)


class TimeSeriesContractTests(unittest.TestCase):
    def test_normalization_round_trip_uses_eight_decimal_places(self):
        source = pd.DataFrame({"Close": [10.0, 15.0, 20.0]})

        normalized = normalize_array(source[["Close"]], 10.0, 20.0)
        restored = reverse_normalize_array(
            np.array([[0.0], [0.5], [1.0]]), 10.0, 20.0)

        self.assertEqual([[0.0], [0.5], [1.0]], normalized)
        self.assertEqual([10.0, 15.0, 20.0], restored)
        self.assertEqual(0.33333333, normalize(1, 0, 3, 0, 1))

    def test_next_timestamp_repeats_the_previous_interval(self):
        result = get_next_datetime(
            "01/08/2026 10:00:00", "01/08/2026 10:05:00")

        self.assertEqual("01/08/2026 10:10:00", result.strftime("%d/%m/%Y %H:%M:%S"))

    def test_next_timestamp_accepts_the_legacy_us_date_format(self):
        result = get_next_datetime(
            "08/16/2026 10:00:00", "08/16/2026 10:05:00")

        self.assertEqual(
            "08/16/2026 10:10:00", result.strftime("%m/%d/%Y %H:%M:%S"))

    def test_normalizer_metadata_round_trips_through_json(self):
        with tempfile.TemporaryDirectory() as directory:
            normalizer = Path(directory) / "btc-eur_normalizer.json"
            with patch(
                    "app.tasks.Configuration.get_file_normalized_file",
                    return_value=str(normalizer)):
                created = load_normalized_data_from_json("btc-eur.csv", 10.0, 20.0)
                loaded = load_normalized_data_from_json("btc-eur.csv")

            self.assertEqual(created, loaded)
            self.assertEqual(
                {
                    "indicator_id": "btc-eur.csv",
                    "min_close_value": 10.0,
                    "max_close_value": 20.0,
                    "epochs": 3550,
                },
                json.loads(normalizer.read_text(encoding="utf-8")))

    def test_normalizer_expands_but_does_not_shrink_stored_bounds(self):
        with tempfile.TemporaryDirectory() as directory:
            normalizer = Path(directory) / "btc-eur_normalizer.json"
            normalizer.write_text(
                json.dumps({
                    "indicator_id": "btc-eur.csv",
                    "min_close_value": 10.0,
                    "max_close_value": 20.0,
                    "epochs": 3550,
                }),
                encoding="utf-8")
            with patch(
                    "app.tasks.Configuration.get_file_normalized_file",
                    return_value=str(normalizer)):
                unchanged = load_normalized_data_from_json(
                    "btc-eur.csv", 12.0, 18.0)
                expanded = load_normalized_data_from_json(
                    "btc-eur.csv", 8.0, 22.0)

            self.assertEqual(10.0, unchanged["min_close_value"])
            self.assertEqual(20.0, unchanged["max_close_value"])
            self.assertEqual(8.0, expanded["min_close_value"])
            self.assertEqual(22.0, expanded["max_close_value"])
            persisted = json.loads(normalizer.read_text(encoding="utf-8"))
            self.assertEqual(8.0, persisted["min_close_value"])
            self.assertEqual(22.0, persisted["max_close_value"])

    def test_chart_contract_preserves_name_order_and_values(self):
        chart = convert_to_chart(
            "close_price", ["t1", "t2"], [101.25, 102.5])

        self.assertEqual(
            {
                "name": "close_price",
                "series": [
                    {"name": "t1", "value": 101.25},
                    {"name": "t2", "value": 102.5},
                ],
            },
            chart)

    def test_sequence_split_is_ordered_and_reshaped_for_lstm(self):
        inputs = [np.array([index] * 5) for index in range(8)]
        outputs = [np.array([index + 1]) for index in range(8)]

        result = get_reshaped_input_and_output_data(inputs, outputs, 5)

        self.assertEqual((6, 5, 1), result["train_set_input"].shape)
        self.assertEqual((6, 1), result["train_set_output"].shape)
        self.assertEqual((2, 5, 1), result["test_set_input"].shape)
        self.assertEqual((2, 1), result["test_set_output"].shape)
        self.assertEqual(0, result["train_set_input"][0, 0, 0])
        self.assertEqual(7, result["test_set_input"][-1, 0, 0])

    @patch("app.tasks.task_work.Configuration.get_file_model_file")
    @patch("app.tasks.task_work.keras.models.load_model")
    def test_prediction_boundary_loads_the_indicator_model(self, load_model, model_file):
        model_file.return_value = "static/btc_eur/btc-eur_model.h5"
        model = Mock()
        model.summary.return_value = "model-summary"
        model.predict.return_value = np.array([[0.75]])
        load_model.return_value = model
        model_input = np.array([[[0.1], [0.2], [0.3], [0.4], [0.5]]])

        result = lstm_prediction("btc-eur.csv", model_input)

        model_file.assert_called_once_with("btc-eur.csv")
        load_model.assert_called_once_with(
            "static/btc_eur/btc-eur_model.h5", compile=False)
        model.predict.assert_called_once_with(model_input)
        np.testing.assert_array_equal(np.array([[0.75]]), result["predicted_price"])


if __name__ == "__main__":
    unittest.main()
