// PERSONA 3 — System Tests: Payment flow + skenarë negativ
import { test, expect } from './fixtures/base-fixture';

test.describe('Pagesat (Payment UI)', () => {
  test.todo('Client paguan plotësisht — Booking bëhet Completed');
  test.todo('Client paguan pjesërisht — Booking mbetet aktiv, balance e re shfaqet');
  test.todo('Pagesë e dytë plotëson — mbyllje automatike');
});

test.describe('Skenarë Negativ', () => {
  test.todo('Client tenton /manager route — ridrejtohet (akses i mohuar)');
  test.todo('Client tenton pagesë mbi balancë — mesazh gabimi specifik');
  test.todo('Pagesë pa Invoice — mesazh "Fatura nuk ekziston"');
});
