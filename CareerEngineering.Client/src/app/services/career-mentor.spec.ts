import { TestBed } from '@angular/core/testing';

import { CareerMentor } from './career-mentor';

describe('CareerMentor', () => {
  let service: CareerMentor;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CareerMentor);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
