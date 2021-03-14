import { Component, OnInit, ViewEncapsulation } from "@angular/core";
import { Router } from "@angular/router";
import { MainService } from "@services/main.service";
import { NotificationContainer } from "@interfaces/notification-container.interface";
import { NgRedux, select } from "@angular-redux/store";
import { Observable } from "rxjs";
import { ExchangeUIContainer } from "@interfaces/exchange-ui-container.interface";
import { DisplayContainer } from "@interfaces/display-container.interface";
import { AnalyticsDataContainer } from "@interfaces/analytics-data-container";
import * as AnalyticsDataContainerActions from "@actions/analytics-data-container.actions";
import * as PredictionContainerActions from "@actions/prediction-container.actions";
import { AppState } from "@store/app.state";
import { FormControl } from "@angular/forms";
import { IndicatorInformation } from "@interfaces/indicator-information";
import { PredictionResult } from "@interfaces/prediction-result.interface";
import { PredictionTaskStatus } from "@interfaces/prediction-task-status.interface";
import { PredictionContainer } from "@interfaces/prediction-container.interface";
import { PredictionData } from "@interfaces/prediction-data.interface";
import { PredictionDataSeries } from "@interfaces/prediction-data-series.interface";

@Component({
    templateUrl: "./machinelearning.component.html"
})
/**
 * TradeComponent - The Trade page
 */
export class MachineLearningComponent implements OnInit {
    @select("notificationContainer") notificationContainer$: Observable<NotificationContainer>;
    notificationContainer: NotificationContainer;
    @select("exchangeUIContainer") exchangeUIContainer$: Observable<ExchangeUIContainer>;
    exchangeUIContainer: ExchangeUIContainer;
    @select("displayContainer") displayContainer$: Observable<DisplayContainer>;
    displayContainer: DisplayContainer;
    @select("analyticsDataContainer") analyticsDataContainer$: Observable<AnalyticsDataContainer>;
    analyticsDataContainer: AnalyticsDataContainer;
    @select("predictionContainer") predictionContainer$: Observable<PredictionContainer>;
    predictionContainer: PredictionContainer;
    indicatorsFormControl: FormControl;
    indicators: string[];

    view: any[] = [700, 800];

    // options
    legend: boolean = true;
    showLabels: boolean = true;
    animations: boolean = true;
    xAxis: boolean = true;
    yAxis: boolean = true;
    showYAxisLabel: boolean = true;
    showXAxisLabel: boolean = true;
    xAxisLabel: string = 'Year';
    yAxisLabel: string = 'Population';
    timeline: boolean = true;
    multi: Array<PredictionData> = []
    yScaleMin: number = undefined;
    yScaleMax: number = undefined;

    colorScheme = {
        domain: ['#5AA454', '#E44D25']
    };

    /**
     * DashboardComponent - Constructor call on initialisation
     * @param router - Router to help us navigate to different pages
     */
    constructor(private router: Router, private ngRedux: NgRedux<AppState>, private mainService: MainService) {
        this.indicatorsFormControl = new FormControl({ value: '', disabled: true });
        this.indicators = new Array();
        this.yScaleMin = undefined;
        this.yScaleMax = undefined;
        this.multi = new Array();
    }
    /**
     * Function called after the constructor and initial ngOnChanges()
     */
    ngOnInit() {
        this.notificationContainer$.subscribe((x: NotificationContainer) => {
            this.notificationContainer = x;
        });
        this.exchangeUIContainer$.subscribe((x: ExchangeUIContainer) => {
            this.exchangeUIContainer = x;
        });
        this.displayContainer$.subscribe((x: DisplayContainer) => {
            this.displayContainer = x;
        });
        this.predictionContainer$.subscribe((x: PredictionContainer) => {
            this.predictionContainer = x;
            let multi = [this.predictionContainer.result.status.close_price,
                this.predictionContainer.result.status.close_price_prediction]
            multi.forEach((element: PredictionData) => {
                let arrayOfValues:number[] = element.series.map((d:PredictionDataSeries) => Number(d.value));
                let minValue = Math.min(...arrayOfValues);
                if(this.yScaleMin == undefined || this.yScaleMin > minValue)
                    this.yScaleMin = minValue;
                let maxValue = Math.max(...arrayOfValues);
                if(this.yScaleMax == undefined || maxValue > this.yScaleMax)
                    this.yScaleMax = maxValue;
            });
            console.log(this.yScaleMin);
            console.log(this.yScaleMax);
            this.multi = multi;
        });
        this.analyticsDataContainer$.subscribe((x: AnalyticsDataContainer) => {
            this.analyticsDataContainer = x;
            this.analyticsDataContainer.indicator_information.indicator_files.forEach((indicator: string) => {
                let index: number = this.indicators.findIndex((current_indicator: string) => {
                    return indicator === current_indicator;
                });
                if (index === -1) {
                    this.indicators.push(indicator);
                }
                this.indicatorsFormControl.enable();
            });
        });
    }

    onSelect(data): void {
        console.log("Item clicked", JSON.parse(JSON.stringify(data)));
    }

    onActivate(data): void {
        console.log("Activate", JSON.parse(JSON.stringify(data)));
    }

    onDeactivate(data): void {
        console.log("Deactivate", JSON.parse(JSON.stringify(data)));
    }

    onGetIndicatorFilenames() {
        this.mainService.get_indicator_filenames()
            .subscribe((indicator_information: IndicatorInformation) => {
                let analyticsDataContainerActions: AnalyticsDataContainerActions.Actions = new AnalyticsDataContainerActions.CRUDAnalyticsDataContainer(
                    this.analyticsDataContainer
                );
                analyticsDataContainerActions.updateIndicatorNameList(indicator_information);
                this.ngRedux.dispatch({
                    type: analyticsDataContainerActions.type,
                    payload: analyticsDataContainerActions.payload,
                });
            },
                (error: any) => {
                    //ERROR
                });
    }

    onPredict() {
        if (this.indicatorsFormControl.value == '' || this.indicatorsFormControl.value == undefined)
            return;
        this.yScaleMin = undefined;
        this.yScaleMax = undefined;
        this.multi = new Array();
        this.mainService.post_run_predict(this.indicatorsFormControl.value)
            .subscribe((prediction_task_status: PredictionTaskStatus) => {
                let predictionContainerActions: PredictionContainerActions.Actions = new PredictionContainerActions.CRUDPredictionContainer(
                    this.predictionContainer
                );
                predictionContainerActions.updateTaskStatusResult(prediction_task_status);
                this.ngRedux.dispatch({
                    type: predictionContainerActions.type,
                    payload: predictionContainerActions.payload,
                });
            },
                (error: any) => {
                    //ERROR
                });
    }

    onRunTraining() {
        if (this.indicatorsFormControl.value == '' || this.indicatorsFormControl.value == undefined)
            return;
        this.mainService.post_run_rnn(this.indicatorsFormControl.value)
            .subscribe((prediction_task_status: PredictionTaskStatus) => {
                let predictionContainerActions: PredictionContainerActions.Actions = new PredictionContainerActions.CRUDPredictionContainer(
                    this.predictionContainer
                );
                predictionContainerActions.updateTaskStatusResult(prediction_task_status);
                this.ngRedux.dispatch({
                    type: predictionContainerActions.type,
                    payload: predictionContainerActions.payload,
                });
            },
                (error: any) => {
                    //ERROR
                });
    }

    onGetTaskResult() {
        if (this.predictionContainer == null)
            return;
        if (this.predictionContainer.task_status.task_id == '' || this.predictionContainer.task_status.task_id == undefined)
            return;
        this.getTaskStatus();
    }

    private getTaskStatus() {
        this.mainService.post_task_status(this.predictionContainer.task_status.task_id)
            .subscribe((prediction_result: PredictionResult) => {
                let predictionContainerActions: PredictionContainerActions.Actions = new PredictionContainerActions.CRUDPredictionContainer(
                    this.predictionContainer
                );
                predictionContainerActions.updatePredictionResult(prediction_result);
                this.ngRedux.dispatch({
                    type: predictionContainerActions.type,
                    payload: predictionContainerActions.payload,
                });
            },
                (error: any) => {
                    //ERROR
                });
    }
}
