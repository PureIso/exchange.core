import sys
from app.tasks.task_work import training, celery
from flask_restful import Resource, reqparse
from flask import Response, jsonify, json


class TaskStatus(Resource):
    def __init__(self):
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('task_id', type=str, location='values')
        super(TaskStatus, self).__init__()

    def get(self):
        try:
            args = self.reqparse.parse_args()
            task_id = str(args['task_id'])
            task = celery.AsyncResult(id=task_id, app=training)

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
            response = Response(message,
                                status=200,  # Status OK
                                mimetype='application/json')
        except:
            message = json.dumps({"error": str(sys.exc_info())})
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response
