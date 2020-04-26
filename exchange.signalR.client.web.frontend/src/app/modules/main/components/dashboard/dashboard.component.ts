import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { MainService } from "@services/main.service";

@Component({
  templateUrl: "./dashboard.component.html",
  styleUrls: ["./dashboard.component.css"]
})

/**
 * DashboardComponent - The dashboard page
 */
export class DashboardComponent implements OnInit {
  /**
   * DashboardComponent - Constructor call on initialisation
   * @param router - Router to help us navigate to different pages
   */
  constructor(private router: Router, private mainService: MainService) { }
  /**
   * Function called after the constructor and initial ngOnChanges()
   */
  ngOnInit() { }

}
