import api from './api';

export const bookingService = {
  async getAll() {
    const response = await api.get('/BookingsApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/BookingsApi/${id}`);
    return response.data;
  },

  async create(booking) {
    const response = await api.post('/BookingsApi', booking);
    return response.data;
  },

  async update(id, booking) {
    await api.put(`/BookingsApi/${id}`, booking);
  },

  // Kthen response.data nga server (p.sh. { message, status, statusName })
  async cancel(id) {
    const response = await api.post(`/BookingsApi/${id}/cancel`);
    return response.data;
  },
};