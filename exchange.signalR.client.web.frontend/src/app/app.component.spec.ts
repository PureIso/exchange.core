import { TestBed, async, ComponentFixture } from "@angular/core/testing";
import { AppComponent } from "./app.component";
import { RouterTestingModule } from "@angular/router/testing";
/**Fields declarations */
let fixture: ComponentFixture<AppComponent>;
let component: AppComponent;
/**
 * Configuration of the Component for DOM testing
 */
function setup() {
    //Inject configuration before every test
    TestBed.configureTestingModule({
        declarations: [AppComponent],
        imports: [RouterTestingModule]
    })
        .compileComponents()
        .then(() => {
            fixture = TestBed.createComponent(AppComponent);
            component = fixture.componentInstance;
        });
}
/**
 * Confirm fixture exists
 */
function componentToBeDefined() {
    expect(component).toBeDefined();
}
/**
 * AppComponent - Test Suite
 */
describe("AppComponent", () => {
    //async will not allow the next test to start until the async finishes all its tasks.
    beforeEach(async(setup));
    it("should create", componentToBeDefined);
});
