import React, { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { bookingService } from '../services/bookingService';
import { workOrderService } from '../services/workOrderService';
import { scheduleService } from '../services/scheduleService';
import { useAuth } from '../contexts/AuthContext';
import { WorkOrderStatus } from '../types';

const MechanicDashboard = () => {
  const { user } = useAuth();
  const [bookings, setBookings] = useState([]);
  const [workOrders, setWorkOrders] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('workorders');
  const [editingWorkOrder, setEditingWorkOrder] = useState(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      setBookings(await bookingService.getAll());
      setWorkOrders(await workOrderService.getAll());
      setSchedules(await scheduleService.getAll());
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateWorkOrder = async (workOrder) => {
    try {
      await workOrderService.update(workOrder.id, workOrder);
      setEditingWorkOrder(null);
      loadData();
    } catch (error) {
      console.error('Error updating work order:', error);
      alert('Error updating work order');
    }
  };

  const tabs = [
    { id: 'workorders', label: 'My Work Orders' },
    { id: 'bookings', label: 'My Bookings' },
    { id: 'schedule', label: 'My Schedule' },
  ];

  return (
    <Layout>
      <div className="px-4 py-6">
        <h1 className="text-3xl font-bold text-gray-900 mb-6">Mechanic Dashboard</h1>

        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 border-b-2 font-medium text-sm ${
                  activeTab === tab.id
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        <div className="mt-6">
          {loading ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <div className="bg-white shadow overflow-hidden sm:rounded-md">
              {activeTab === 'workorders' && (
                <WorkOrdersSection
                  workOrders={workOrders}
                  onEdit={setEditingWorkOrder}
                  onUpdate={handleUpdateWorkOrder}
                />
              )}
              {activeTab === 'bookings' && <BookingsSection bookings={bookings} />}
              {activeTab === 'schedule' && <ScheduleSection schedules={schedules} />}
            </div>
          )}
        </div>

        {editingWorkOrder && (
          <WorkOrderModal
            workOrder={editingWorkOrder}
            onClose={() => setEditingWorkOrder(null)}
            onSave={handleUpdateWorkOrder}
          />
        )}
      </div>
    </Layout>
  );
};

const WorkOrdersSection = ({ workOrders, onEdit, onUpdate }) => (
  <ul className="divide-y divide-gray-200">
    {workOrders.map((wo) => (
      <li key={wo.id} className="px-6 py-4">
        <div className="flex justify-between items-start">
          <div className="flex-1">
            <div className="font-medium text-gray-900">Work Order #{wo.id}</div>
            {wo.booking && (
              <>
                <div className="text-sm font-semibold text-emerald-700 mt-1">
                  Klienti: {wo.booking.client?.firstName} {wo.booking.client?.lastName}
                </div>
                {wo.booking.vehicle && (
                  <div className="text-sm text-gray-600 mt-1">
                    Makina: {wo.booking.vehicle.make} {wo.booking.vehicle.model} - {wo.booking.vehicle.licensePlate}
                  </div>
                )}
                <div className="text-sm text-gray-500 mt-1">
                  Data: {new Date(wo.booking.bookingDate).toLocaleDateString()} në {wo.booking.bookingTime}
                </div>
              </>
            )}
            <div className="text-sm text-gray-500 mt-1">
              Status: {wo.status === 0 ? 'Scheduled' : wo.status === 1 ? 'In Progress' : wo.status === 2 ? 'Completed' : wo.status === 3 ? 'Ready For Payment' : 'Closed'}
            </div>
            {wo.description && (
              <div className="text-sm text-gray-600 mt-1">Description: {wo.description}</div>
            )}
            {wo.mechanicNotes && (
              <div className="text-sm text-gray-600 mt-2 bg-gray-50 p-2 rounded">
                <strong>Shënime:</strong> {wo.mechanicNotes}
              </div>
            )}
            {wo.totalCost && (
              <div className="text-sm font-semibold text-blue-700 mt-2">
                Total Cost: ${parseFloat(wo.totalCost).toFixed(2)}
              </div>
            )}
          </div>
          <button
            onClick={() => onEdit(wo)}
            className="bg-primary-600 hover:bg-primary-700 text-white px-4 py-2 rounded-md text-sm ml-4"
          >
            Update Status
          </button>
        </div>
      </li>
    ))}
  </ul>
);

const BookingsSection = ({ bookings }) => (
  <ul className="divide-y divide-gray-200">
    {bookings.map((booking) => (
      <li key={booking.id} className="px-6 py-4">
        <div className="font-medium text-gray-900">Booking #{booking.id}</div>
        {booking.client && (
          <div className="text-sm font-semibold text-emerald-700 mt-1">
            Klienti: {booking.client.firstName} {booking.client.lastName}
          </div>
        )}
        {booking.vehicle && (
          <div className="text-sm text-gray-600 mt-1">
            Makina: {booking.vehicle.licensePlate}
          </div>
        )}
        <div className="text-sm text-gray-500 mt-1">
          Data: {new Date(booking.bookingDate).toLocaleDateString()} në {booking.bookingTime}
        </div>
        <div className="text-sm text-gray-500 mt-1">
          Status: {booking.status === 0 ? 'Pending' : booking.status === 1 ? 'Confirmed' : booking.status === 2 ? 'In Progress' : booking.status === 3 ? 'Completed' : 'Cancelled'}
        </div>
      </li>
    ))}
  </ul>
);

const ScheduleSection = ({ schedules }) => (
  <ul className="divide-y divide-gray-200">
    {schedules.map((schedule) => (
      <li key={schedule.id} className="px-6 py-4">
        <div className="font-medium text-gray-900">
          Day: {['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][schedule.dayOfWeek]}
        </div>
        <div className="text-sm text-gray-500">
          Time: {schedule.startTime} - {schedule.endTime}
        </div>
        <div className="text-sm text-gray-500">
          Available: {schedule.isAvailable ? 'Yes' : 'No'}
        </div>
      </li>
    ))}
  </ul>
);

const WorkOrderModal = ({ workOrder, onClose, onSave }) => {
  const [formData, setFormData] = useState(workOrder);

  const handleSubmit = (e) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
        <h3 className="text-lg font-bold mb-4">Update Work Order</h3>
        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
            <select
              value={formData.status}
              onChange={(e) => setFormData({ ...formData, status: parseInt(e.target.value) })}
              className="w-full px-3 py-2 border rounded-md"
            >
              {Object.entries(WorkOrderStatus)
                .filter(([key]) => isNaN(Number(key)))
                .map(([key, value]) => (
                  <option key={key} value={value}>{key}</option>
                ))}
            </select>
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Mechanic Notes</label>
            <textarea
              value={formData.mechanicNotes || ''}
              onChange={(e) => setFormData({ ...formData, mechanicNotes: e.target.value })}
              className="w-full px-3 py-2 border rounded-md"
              rows={4}
            />
          </div>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Actual Duration (minutes)</label>
            <input
              type="number"
              value={formData.actualDurationMinutes || ''}
              onChange={(e) => setFormData({ ...formData, actualDurationMinutes: parseInt(e.target.value) || undefined })}
              className="w-full px-3 py-2 border rounded-md"
            />
          </div>
          <div className="flex justify-end space-x-2">
            <button type="button" onClick={onClose} className="px-4 py-2 bg-gray-300 rounded-md">
              Cancel
            </button>
            <button type="submit" className="px-4 py-2 bg-primary-600 text-white rounded-md">
              Save
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default MechanicDashboard;

