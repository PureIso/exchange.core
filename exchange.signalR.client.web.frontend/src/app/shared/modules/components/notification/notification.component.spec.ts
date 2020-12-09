import { TestBed, ComponentFixture, waitForAsync } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MockNgRedux, NgReduxTestingModule } from '@angular-redux/store/testing';
import { AppConfig } from '@config/config';
import { AppState } from '@store/app.state';
import * as NotificationContainerReducer from '@reducers/notification-container.reducer';
import { NotificationComponent } from './notification.component';
import { NotificationContainer } from '@interfaces/notification-container.interface';
/**Fields declarations */
let component: NotificationComponent;
let fixture: ComponentFixture<NotificationComponent>;
/**
 * Configuration of the Component for DOM testing
 */
function setup() {
    //Inject configuration before every test
    TestBed.configureTestingModule({
        imports: [
            FormsModule,
            CommonModule,
            RouterTestingModule,
            NgReduxTestingModule
        ],
        declarations: [
            NotificationComponent
        ],
        providers: [
            MockNgRedux,
            AppConfig
        ]
    })
        .compileComponents().then(() => {
            //Initialise redux selectors Subjects with the initial states
            MockNgRedux.getSelectorStub<AppState, NotificationContainer>('notificationContainer').next(NotificationContainerReducer.initialState);
            //Creates an instance of the NotificationComponent, adds a corresponding element to the test-runner DOM, and returns a ChangeUserFixture.
            fixture = TestBed.createComponent(NotificationComponent);
            //Access the component instance through the fixture
            component = fixture.componentInstance;
            //Tell the TestBed to perform data binding by calling
            fixture.detectChanges();
        });
    //Reset all configured stubs
    MockNgRedux.reset();
}
/**
 * Confirm fixture exists
 */
function confirmFixtureExists() {
    expect(component).toBeDefined();
}
/**
 * NotificationComponent - Test Suite
 */
describe('NotificationComponent', () => {
    //async will not allow the next test to start until the async finishes all its tasks.
    beforeEach(waitForAsync(setup));
    it('should create', confirmFixtureExists);
});
