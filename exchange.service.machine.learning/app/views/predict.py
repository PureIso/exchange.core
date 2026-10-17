"""Predict API Controller"""
from flask_restful import Resource, reqparse
from flask import json, Response
from typing import Any
from app.tasks.task_work import prediction
from app.configuration import Configuration


class Predict(Resource):
    """Predict API Class
    Args:
        Resource (Resource): RESTFul resource
    """

    def __init__(self, configuration):
        self.configuration = configuration
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('indicator_file', type=str, location='json')
        super(Predict, self).__init__()

    @staticmethod
    def get():
        """Predict [get] method
        Returns:
            Response: The response
        """
        message = json.dumps({"API VERSION": "1.0.0"})
        response = Response(message,
                            status=200,  # Status OK
                            mimetype='application/json')
        return response

    def post(self) -> Response:
        """predict [post] method
        Returns:
            Response: The response
        """
        args: Any = self.reqparse.parse_args()
        indicator_file: Any = args['indicator_file']
        if not Configuration.is_valid_indicator_file(indicator_file):
            response: Response = Response(
                json.dumps({"error": "invalid_indicator_file"}),
                status=400,
                mimetype='application/json')
            return response
        task: Any = prediction.delay(indicator_file)
        message: str = json.dumps({
            "task_id": str(task.task_id),
            "status": str(task.status)})

        self.configuration.mi_logger.info(message)
        response: Response = Response(message,
                            status=202,  # Status Accepted
                            mimetype='application/json')
        return response
