import api from './api';

export const invoiceService = {
  async getAll() {
    const response = await api.get('/InvoicesApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/InvoicesApi/${id}`);
    return response.data;
  },

  async getByWorkOrder(workOrderId) {
    const response = await api.get(`/InvoicesApi/workorder/${workOrderId}`);
    return response.data;
  },

  async create(invoiceData) {
    const response = await api.post('/InvoicesApi', invoiceData);
    return response.data;
  },
};

