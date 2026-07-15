import { TestBed } from '@angular/core/testing';

import { SignalR } from './signal-r';

describe('SignalR', () => {
  let service: SignalR;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SignalR);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
