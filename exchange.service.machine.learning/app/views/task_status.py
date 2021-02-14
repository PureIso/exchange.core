"""Celery Task Status"""
from flask_restful import Resource, reqparse
from flask import Response, json
from app.tasks.task_work import training


class TaskStatus(Resource):
    """Task Status class
    Args:
        Resource (Resource): RESTFul resource
    """
    def __init__(self, configuration):
        self.configuration = configuration
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('task_id', type=str, location='values')
        super(TaskStatus, self).__init__()

    def get(self):
        """Task Status [get] method
        Returns:
            Response: The response
        """
        args = self.reqparse.parse_args()
        task_id = str(args['task_id'])
        task = self.configuration.celery.AsyncResult(id=task_id, app=training)

        if task.state == 'PENDING':
            # job did not start yet
            response = {
                'state': task.state,
                'status': 'Pending...'
            }
        else:
            response = {
                'state': task.state,
                'status': str(json.dumps(str(task.info))),
            }
        message = json.dumps(response)
        self.configuration.mi_logger.info(message)
        response = Response(message,
                            status=200,  # Status OK
                            mimetype='application/json')
        return response
