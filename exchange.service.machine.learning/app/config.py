import os


class Config():
    NORMALISEJSONFILENAME = 'normalizer.json'
    MODELH5FILENAME = 'model.h5'
    DAILYCSVFILENAME = "coinbase_btc_eur_historic_data.csv"
    HOURLYCSVFILENAME = "coinbase_btc_eur_historic_data_hourly.csv"

    def getDailyDirectory(self):
        return "static/daily_data"

    def getHourlyDirectory(self):
        return "static/hourly_data"

    def getHourlyCSVFILE(self):
        return os.path.join(self.getHourlyDirectory(), self.HOURLYCSVFILENAME)

    def getHourlyNormalisJSONFILE(self):
        return os.path.join(self.getHourlyDirectory(), self.NORMALISEJSONFILENAME)

    def getHourlyModelH5FILE(self):
        return os.path.join(self.getHourlyDirectory(), self.MODELH5FILENAME)

    def getDailyCSVFILE(self):
        return os.path.join(self.getDailyDirectory(), self.DAILYCSVFILENAME)

    def getDailyNormalisJSONFILE(self):
        return os.path.join(self.getDailyDirectory(), self.NORMALISEJSONFILENAME)

    def getDailyModelH5FILE(self):
        return os.path.join(self.getDailyDirectory(), self.MODELH5FILENAME)
