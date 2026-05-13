import { Component, OnInit } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TestRubriqueViewModel } from './test-rubrique.viewmodel';

@Component({
  selector: 'app-test-rubrique',
  standalone: true,
  imports: [NgFor, NgIf, FormsModule],
  templateUrl: './test-rubrique.component.html',
  styleUrl: './test-rubrique.component.scss',
  providers: [TestRubriqueViewModel],
})
export class TestRubriqueComponent implements OnInit {
  constructor(public readonly vm: TestRubriqueViewModel) {}

  ngOnInit(): void {
    this.vm.loadAll();
  }
}
