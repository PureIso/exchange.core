from app.exchange import application

if __name__ == '__main__':
    
    application.run(host='exchange.service.machine-learning-service', port=5005, debug=True)
