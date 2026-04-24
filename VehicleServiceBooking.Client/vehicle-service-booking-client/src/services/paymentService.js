import api from './api';

export const paymentService = {
  async getAll() {
    const response = await api.get('/PaymentsApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/PaymentsApi/${id}`);
    return response.data;
  },

  async create(payment) {
    const response = await api.post('/PaymentsApi', payment);
    return response.data;
  },

  async update(id, payment) {
    await api.put(`/PaymentsApi/${id}`, payment);
  },

  async delete(id) {
    await api.delete(`/PaymentsApi/${id}`);
  },
};
