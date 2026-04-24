import '@testing-library/jest-dom';

// localStorage mock — jsdom nuk e ekspozon gjithmonë localStorage nëpër vi environments
const storage = {};
const localStorageMock = {
  getItem: (key) => storage[key] ?? null,
  setItem: (key, value) => { storage[key] = String(value); },
  removeItem: (key) => { delete storage[key]; },
  clear: () => { Object.keys(storage).forEach((k) => delete storage[k]); },
};
Object.defineProperty(globalThis, 'localStorage', {
  value: localStorageMock,
  writable: true,
});

// window.location.href reassignment nuk funksionon në jsdom — stub-o pa error
Object.defineProperty(window, 'location', {
  value: { href: '' },
  writable: true,
});
