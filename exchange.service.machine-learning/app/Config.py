
import os


class Config():
    NORMALISEJSONFILENAME = 'normalizer.json'
    MODELH5FILENAME = 'model.h6'
    DAILYCSVFILENAME = "coinbase_btc_eur_historic_data.csv"
    HOURLYCSVFILENAME = "coinbase_btc_eur_historic_data_hourly.csv"
    MODELH5FILENAME = "model.h5"

    def getHourlyCSVFILE(self):
        return os.path.join("app/static/hourly_data", self.HOURLYCSVFILENAME)

    def getHourlyNormalisJSONFILE(self):
        return os.path.join("app/static/hourly_data", self.NORMALISEJSONFILENAME)

    def getHourlyModelH5FILE(self):
        return os.path.join("app/static/hourly_data", self.MODELH5FILENAME)

    def getDailyCSVFILE(self):
        return os.path.join("app/static/daily_data", self.DAILYCSVFILENAME)

    def getDailyNormalisJSONFILE(self):
        return os.path.join("app/static/daily_data", self.NORMALISEJSONFILENAME)

    def getDailyModelH5FILE(self):
        return os.path.join("app/static/daily_data", self.MODELH5FILENAME)
