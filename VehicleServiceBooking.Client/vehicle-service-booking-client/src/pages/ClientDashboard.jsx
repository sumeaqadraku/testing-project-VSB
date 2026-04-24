import React, { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { vehicleService } from '../services/vehicleService';
import { bookingService } from '../services/bookingService';
import { serviceTypeService } from '../services/serviceTypeService';
import { serviceCenterService } from '../services/serviceCenterService';
import { paymentService } from '../services/paymentService';
import { invoiceService } from '../services/invoiceService';
import { workOrderService } from '../services/workOrderService';
import { useAuth } from '../contexts/AuthContext';
import bgImage from "../assets/photo3.jpg";

const ClientDashboard = () => {
  const { user } = useAuth();
  const [vehicles, setVehicles] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [payments, setPayments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [serviceTypes, setServiceTypes] = useState([]);
  const [serviceCenters, setServiceCenters] = useState([]);
  const [workOrdersByBooking, setWorkOrdersByBooking] = useState({});
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('vehicles');
  const [showVehicleModal, setShowVehicleModal] = useState(false);
  const [showBookingModal, setShowBookingModal] = useState(false);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [editingVehicle, setEditingVehicle] = useState(null);

  const BOOKING_STATUS_MAP = {
    0: 'Pending',
    1: 'Confirmed',
    2: 'Cancelled'
  };

  const WORKORDER_STATUS_MAP = {
    0: 'Scheduled',
    1: 'In Progress',
    2: 'Completed',
    3: 'Ready For Payment',
    4: 'Closed'
  };

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      setVehicles(await vehicleService.getAll());

      const b = await bookingService.getAll();
      
      setBookings(b);

      setPayments(await paymentService.getAll());
      setServiceTypes(await serviceTypeService.getAll(true));
      setServiceCenters(await serviceCenterService.getAll());

      try {
        const workOrders = await workOrderService.getAll();
        const map = {};
        (Array.isArray(workOrders) ? workOrders : []).forEach(wo => {
          if (wo.bookingId != null) map[wo.bookingId] = wo;
        });
        setWorkOrdersByBooking(map);

       
        const invoicePromises = (workOrders || []).map(wo =>
          invoiceService.getByWorkOrder(wo.id).catch(() => null)
        );
        const invoiceResults = await Promise.all(invoicePromises);
        setInvoices(invoiceResults.filter(inv => inv !== null));
      } catch (err) {
        console.error('Error loading workorders/invoices:', err);
      }

    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVehicle = () => {
    setEditingVehicle(null);
    setShowVehicleModal(true);
  };

  const handleEditVehicle = (vehicle) => {
    setEditingVehicle(vehicle);
    setShowVehicleModal(true);
  };

  const handleDeleteVehicle = async (id) => {
    if (!confirm('Are you sure you want to delete this vehicle?')) return;
    try {
      await vehicleService.delete(id);
      loadData();
    } catch (error) {
      const msg = error.response?.data?.message || 'Error deleting vehicle';
      alert(msg);
    }
  };

  const handleSaveVehicle = async (data) => {
    try {
      const vehicleData = {
        Make: data.make,
        Id: editingVehicle ? editingVehicle.id : 0,
        Model: data.model,
        LicensePlate: data.licensePlate,
        Year: data.year ? parseInt(data.year) : null,
        Color: data.color || "",
        VIN: data.vin || ""
      };

      if (editingVehicle) {
        await vehicleService.update(editingVehicle.id, vehicleData);
      } else {
        await vehicleService.create(vehicleData);
      }

      setShowVehicleModal(false);
      loadData();
    } catch (error) {
      const serverErrors = error.response?.data?.errors;
      if (serverErrors) {
        const errorMessages = Object.entries(serverErrors)
          .map(([field, msgs]) => `${field}: ${msgs.join(', ')}`)
          .join('\n');
        alert("Validimi dështoi:\n" + errorMessages);
      } else {
        alert("Gabim gjatë ruajtjes së makinës.");
      }
    }
  };

  const handleCreateBooking = () => {
    setShowBookingModal(true);
  };

  const handleSaveBooking = async (data) => {
    try {
      const bookingData = {
        VehicleId: parseInt(data.vehicleId),
        ServiceTypeId: parseInt(data.serviceTypeId),
        ServiceCenterId: parseInt(data.serviceCenterId),
        BookingDate: data.bookingDate,
        BookingTime: data.bookingTime.length === 5 ? `${data.bookingTime}:00` : data.bookingTime,
        Status: 0,
      };

      await bookingService.create(bookingData);
      setShowBookingModal(false);
      loadData();
    } catch (error) {
      const errorMessage = error.response?.data?.message ||
        (error.response?.data?.errors ? "Format i gabuar i të dhënave" : "Error creating booking");
      alert(errorMessage);
    }
  };

  const handleCancelBooking = async (id, booking) => {
    if (!confirm('Are you sure you want to cancel this booking?')) return;

    try {
      if (!booking) booking = await bookingService.getById(id);

      const parseBookingDateTime = (b) => {
        if (!b) return null;
        try {
          const datePart = (b.bookingDate || '').split('T')[0];
          let time = b.bookingTime || '';
          if (time.length === 5) time = `${time}:00`;
          if (!time) return new Date(b.bookingDate);
          return new Date(`${datePart}T${time}`);
        } catch {
          return new Date(b.bookingDate || b);
        }
      };

      const bookingDateTime = parseBookingDateTime(booking);
      if (bookingDateTime && !isNaN(bookingDateTime.getTime())) {
        const hoursUntil = (bookingDateTime - new Date()) / (1000 * 60 * 60);
        if (hoursUntil < 24) {
          alert('Cannot cancel booking: minimum 24 hours notice required.');
          return;
        }
      } else {
        console.warn('Could not parse booking datetime locally, attempting server cancel.');
      }

     
      const resp = await bookingService.cancel(id); 
      if (resp?.message) alert(resp.message);
      else alert('Booking cancelled successfully.');

      loadData();
    } catch (error) {
      const msg = error.response?.data?.message || error.message || 'Error cancelling booking';
      alert(msg);
    }
  };

  const tabs = [
    { id: 'vehicles', label: 'My Vehicles' },
    { id: 'bookings', label: 'My Bookings' },
    { id: 'payments', label: 'Payments & Invoices' },
  ];

  return (
    <Layout>
  <div className="px-4 py-6">
        <h1 className="text-3xl font-bold text-primary-600">PERSHËNDETJE!</h1>

        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8">
            {tabs.map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm ${activeTab === tab.id ? 'border-primary-500 text-primary-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        <div className="mt-6">
          {activeTab === 'vehicles' && (
            <div className="mb-4">
              <button onClick={handleCreateVehicle} className="bg-primary-600 hover:bg-primary-700 text-white px-4 py-2 rounded-md">Add Vehicle</button>
            </div>
          )}

          {activeTab === 'bookings' && (
            <div className="mb-4">
              <button onClick={handleCreateBooking} className="bg-primary-600 hover:bg-primary-700 text-white px-4 py-2 rounded-md">New Booking</button>
            </div>
          )}

          {loading ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <div className="bg-white shadow overflow-hidden sm:rounded-md">
              {activeTab === 'vehicles' && (
                <VehiclesList vehicles={vehicles} onEdit={handleEditVehicle} onDelete={handleDeleteVehicle} />
              )}
              {activeTab === 'bookings' && (
                <BookingsList
                  bookings={bookings}
                  onCancel={handleCancelBooking}
                  bookingStatusMap={BOOKING_STATUS_MAP}
                  workOrdersByBooking={workOrdersByBooking}
                  workOrderStatusMap={WORKORDER_STATUS_MAP}
                />
              )}
              {activeTab === 'payments' && (
                <PaymentsSection invoices={invoices} payments={payments} onPay={(invoice) => { setSelectedInvoice(invoice); setShowPaymentModal(true); }} />
              )}
            </div>
          )}
        </div>

        {showVehicleModal && <VehicleModal vehicle={editingVehicle} onClose={() => setShowVehicleModal(false)} onSave={handleSaveVehicle} />}

        {showBookingModal && <BookingModal vehicles={vehicles} serviceTypes={serviceTypes} serviceCenters={serviceCenters} onClose={() => setShowBookingModal(false)} onSave={handleSaveBooking} />}

        {showPaymentModal && selectedInvoice && (
          <PaymentModal invoice={selectedInvoice} onClose={() => { setShowPaymentModal(false); setSelectedInvoice(null); }} onSave={async (paymentData) => {
            try {
              await paymentService.create(paymentData);
              alert('Payment submitted successfully!');
              setShowPaymentModal(false);
              setSelectedInvoice(null);
              loadData();
            } catch (error) {
              alert(error.response?.data?.message || 'Error submitting payment');
            }
          }} />
        )}
      </div>
    </Layout>
  );
};

const VehiclesList = ({ vehicles, onEdit, onDelete }) => (
  <ul className="divide-y divide-gray-200">
    {vehicles.map(vehicle => (
      <li key={vehicle.id} className="px-6 py-4 flex justify-between items-center">
        <div>
          <div className="font-medium text-gray-900">{vehicle.make} {vehicle.model} ({vehicle.year || 'N/A'})</div>
          <div className="text-sm text-gray-500">License: {vehicle.licensePlate}</div>
        </div>
        <div className="flex space-x-2">
          <button onClick={() => onEdit(vehicle)} className="text-primary-600 hover:text-primary-800">Edit</button>
          <button onClick={() => onDelete(vehicle.id)} className="text-red-600 hover:text-red-800">Delete</button>
        </div>
      </li>
    ))}
  </ul>
);

const BookingsList = ({ bookings, onCancel, bookingStatusMap, workOrdersByBooking, workOrderStatusMap }) => (
  <ul className="divide-y divide-gray-200">
    {bookings.map(booking => {
      let bookingDateTime = null;
      try {
        if (booking.bookingTime) {
          const time = booking.bookingTime.length === 5 ? `${booking.bookingTime}:00` : booking.bookingTime;
          const datePart = (booking.bookingDate || '').split('T')[0];
          bookingDateTime = new Date(`${datePart}T${time}`);
        } else {
          bookingDateTime = new Date(booking.bookingDate);
        }
      } catch {
        bookingDateTime = null;
      }

      const hoursUntil = bookingDateTime ? (bookingDateTime - new Date()) / (1000 * 60 * 60) : Infinity;

     
      const relatedWO = workOrdersByBooking?.[booking.id];
      const statusLabel = relatedWO
        ? (workOrderStatusMap?.[relatedWO.status] || String(relatedWO.status))
        : (booking.statusName || bookingStatusMap?.[booking.status] || String(booking.status ?? 'Unknown'));

      const isCancelled = (booking.statusName && booking.statusName.toLowerCase() === 'cancelled') || booking.status === 2;
      
const canCancel = booking.status === 0;

      const timeDisplay = booking.bookingTime ? booking.bookingTime.substring(0,5) : '';

      return (
        <li key={booking.id} className="px-6 py-4">
          <div className="flex justify-between items-start">
            <div>
              <div className="font-medium text-gray-900">Booking #{booking.id}</div>
              <div className="text-sm text-gray-500">Date: {booking.bookingDate ? new Date(booking.bookingDate).toLocaleDateString() : 'N/A'} at {timeDisplay}</div>
              <div className="text-sm text-gray-500">Status: {statusLabel}</div>
            </div>
            {canCancel && (
              <button onClick={() => onCancel(booking.id, booking)} className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-md text-sm">Cancel</button>
            )}
          </div>
        </li>
      );
    })}
  </ul>
);

const VehicleModal = ({ vehicle, onClose, onSave }) => {
  const [formData, setFormData] = useState(vehicle || { make: '', model: '', licensePlate: '', year: '', color: '' });

  const handleSubmit = (e) => { e.preventDefault(); onSave(formData); };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
        <h3 className="text-lg font-bold mb-4">{vehicle ? 'Edit' : 'Add'} Vehicle</h3>
        <form onSubmit={handleSubmit}>
          <div className="mb-4"><input type="text" placeholder="Make" value={formData.make || ''} onChange={(e) => setFormData({ ...formData, make: e.target.value })} className="w-full px-3 py-2 border rounded-md" required /></div>
          <div className="mb-4"><input type="text" placeholder="Model" value={formData.model || ''} onChange={(e) => setFormData({ ...formData, model: e.target.value })} className="w-full px-3 py-2 border rounded-md" required /></div>
          <div className="mb-4"><input type="text" placeholder="License Plate" value={formData.licensePlate || ''} onChange={(e) => setFormData({ ...formData, licensePlate: e.target.value })} className="w-full px-3 py-2 border rounded-md" required /></div>
          <div className="mb-4"><input type="text" placeholder="Year" value={formData.year || ''} onChange={(e) => setFormData({ ...formData, year: e.target.value })} className="w-full px-3 py-2 border rounded-md" /></div>
          <div className="mb-4"><input type="text" placeholder="Color" value={formData.color || ''} onChange={(e) => setFormData({ ...formData, color: e.target.value })} className="w-full px-3 py-2 border rounded-md" /></div>
          <div className="flex justify-end space-x-2">
            <button type="button" onClick={onClose} className="px-4 py-2 bg-gray-300 rounded-md">Cancel</button>
            <button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md">Save</button>
          </div>
        </form>
      </div>
    </div>
  );
};

const BookingModal = ({ vehicles, serviceTypes, serviceCenters, onClose, onSave }) => {
  const [formData, setFormData] = useState({ vehicleId: vehicles[0]?.id, serviceTypeId: serviceTypes[0]?.id, serviceCenterId: serviceCenters[0]?.id, bookingDate: new Date().toISOString().split('T')[0], bookingTime: '09:00' });

  const handleSubmit = (e) => { e.preventDefault(); onSave(formData); };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
        <h3 className="text-lg font-bold mb-4">New Booking</h3>
        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Vehicle</label>
            <select value={formData.vehicleId || ''} onChange={(e) => setFormData({ ...formData, vehicleId: parseInt(e.target.value) })} className="w-full px-3 py-2 border rounded-md" required>
              {vehicles.map(v => <option key={v.id} value={v.id}>{v.make} {v.model} - {v.licensePlate}</option>)}
            </select>
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Service Type</label>
            <select value={formData.serviceTypeId || ''} onChange={(e) => setFormData({ ...formData, serviceTypeId: parseInt(e.target.value) })} className="w-full px-3 py-2 border rounded-md" required>
              {serviceTypes.map(st => <option key={st.id} value={st.id}>{st.name} - ${st.basePrice}</option>)}
            </select>
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Service Center</label>
            <select value={formData.serviceCenterId || ''} onChange={(e) => setFormData({ ...formData, serviceCenterId: parseInt(e.target.value) })} className="w-full px-3 py-2 border rounded-md" required>
              {serviceCenters.map(sc => <option key={sc.id} value={sc.id}>{sc.name}</option>)}
            </select>
          </div>
          <div className="mb-4"><label className="block text-sm font-medium text-gray-700 mb-1">Date</label><input type="date" value={formData.bookingDate || ''} onChange={(e) => setFormData({ ...formData, bookingDate: e.target.value })} className="w-full px-3 py-2 border rounded-md" required /></div>
          <div className="mb-4"><label className="block text-sm font-medium text-gray-700 mb-1">Time</label><input type="time" value={formData.bookingTime || ''} onChange={(e) => setFormData({ ...formData, bookingTime: e.target.value })} className="w-full px-3 py-2 border rounded-md" required /></div>
          <div className="flex justify-end space-x-2"><button type="button" onClick={onClose} className="px-4 py-2 bg-gray-300 rounded-md">Cancel</button><button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md">Create Booking</button></div>
        </form>
      </div>
    </div>
  );
};

const PaymentsSection = ({ invoices, payments, onPay }) => {
  const getTotalPaid = (workOrderId) => payments.filter(p => p.workOrderId === workOrderId && p.status === 1).reduce((sum,p) => sum + parseFloat(p.amount), 0);

  return (
    <div className="space-y-6 p-6">
      <div>
        <h3 className="text-lg font-bold mb-4">Invoices</h3>
        <ul className="divide-y divide-gray-200">
          {invoices.map(inv => {
            const totalPaid = getTotalPaid(inv.workOrderId);
            const remaining = parseFloat(inv.totalAmount) - totalPaid;
            return (
              <li key={inv.id} className="py-4">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="font-medium text-gray-900">Invoice #{inv.invoiceNumber}</div>
                    <div className="text-sm text-gray-500 mt-1">Work Order ID: {inv.workOrderId}</div>
                    <div className="text-sm text-gray-600 mt-2"><div>Subtotal: ${parseFloat(inv.subTotal).toFixed(2)}</div><div>Tax: ${parseFloat(inv.taxAmount).toFixed(2)}</div><div className="font-semibold">Total: ${parseFloat(inv.totalAmount).toFixed(2)}</div><div className="text-emerald-600 mt-1">Paid: ${totalPaid.toFixed(2)}</div>{remaining > 0 && <div className="text-red-600 font-semibold mt-1">Remaining: ${remaining.toFixed(2)}</div>}</div>
                  </div>
                  {remaining > 0 ? <button onClick={() => onPay(inv)} className="bg-primary-600 hover:bg-primary-700 text-white px-4 py-2 rounded-md text-sm">Pay Now</button> : <span className="text-green-600 font-semibold">Paid</span>}
                </div>
              </li>
            );
          })}
        </ul>
      </div>

      <div>
        <h3 className="text-lg font-bold mb-4">Payment History</h3>
        <ul className="divide-y divide-gray-200">
          {payments.map(payment => (
            <li key={payment.id} className="py-4">
              <div className="font-medium text-gray-900">Payment #{payment.id}</div>
              <div className="text-sm text-gray-500 mt-1">Work Order ID: {payment.workOrderId}</div>
              <div className="text-sm text-gray-600 mt-1">Amount: ${parseFloat(payment.amount).toFixed(2)}</div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};

const PaymentModal = ({ invoice, onClose, onSave }) => {
  const [formData, setFormData] = useState({ workOrderId: invoice.workOrderId, amount: parseFloat(invoice.totalAmount).toFixed(2), method: 0, transactionId: '', notes: '' });

  const handleSubmit = (e) => { e.preventDefault(); onSave({ ...formData, amount: parseFloat(formData.amount), method: parseInt(formData.method) }); };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
        <h3 className="text-lg font-bold mb-4">Make Payment</h3>
        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Invoice</label>
            <div className="text-sm text-gray-600">
              Invoice #{invoice.invoiceNumber} - Total: ${parseFloat(invoice.totalAmount).toFixed(2)}
            </div>
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Amount</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              max={parseFloat(invoice.totalAmount)}
              value={formData.amount}
              onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
              className="w-full px-3 py-2 border rounded-md"
              required
            />
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Payment Method</label>
            <select
              value={formData.method}
              onChange={(e) => setFormData({ ...formData, method: e.target.value })}
              className="w-full px-3 py-2 border rounded-md"
              required
            >
              <option value="0">Cash</option>
              <option value="1">Credit Card</option>
              <option value="2">Debit Card</option>
              <option value="3">Bank Transfer</option>
              <option value="4">Online</option>
            </select>
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Transaction ID (Optional)</label>
            <input
              type="text"
              value={formData.transactionId}
              onChange={(e) => setFormData({ ...formData, transactionId: e.target.value })}
              className="w-full px-3 py-2 border rounded-md"
            />
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes (Optional)</label>
            <textarea
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              className="w-full px-3 py-2 border rounded-md"
              rows={3}
            />
          </div>
          <div className="flex justify-end space-x-2">
            <button type="button" onClick={onClose} className="px-4 py-2 bg-gray-300 rounded-md">
              Cancel
            </button>
            <button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md">
              Submit Payment
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ClientDashboard;