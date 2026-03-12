import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Shield, RefreshCw, Search, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../components/Card';
import { Button } from '../components/Button';
import { auditApi } from '../api';

export function AuditPage() {
  const [eventTypeFilter, setEventTypeFilter] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['audit', 'events', page, eventTypeFilter],
    queryFn: () =>
      auditApi.listEvents({
        page,
        pageSize: 20,
        eventType: eventTypeFilter || undefined,
      }),
    staleTime: 10_000,
  });

  const { data: integrity, refetch: verifyIntegrity, isFetching: verifying } = useQuery({
    queryKey: ['audit', 'integrity'],
    queryFn: () => auditApi.verifyIntegrity(),
    enabled: false,
  });

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
          <p className="text-gray-500 mt-1">Immutable event trail with SHA-256 hash chain</p>
        </div>
        <Button
          variant="secondary"
          onClick={() => verifyIntegrity()}
          loading={verifying}
        >
          <Shield size={16} />
          Verify Integrity
        </Button>
      </div>

      {/* Integrity result */}
      {integrity && (
        <div
          className={`mb-4 rounded-lg border px-4 py-3 flex items-center gap-3 ${
            integrity.valid
              ? 'border-green-200 bg-green-50'
              : 'border-red-200 bg-red-50'
          }`}
        >
          {integrity.valid ? (
            <CheckCircle size={16} className="text-green-600 shrink-0" />
          ) : (
            <XCircle size={16} className="text-red-600 shrink-0" />
          )}
          <p className={`text-sm ${integrity.valid ? 'text-green-700' : 'text-red-700'}`}>
            {integrity.message}
          </p>
        </div>
      )}

      {/* Filter */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-center">
            <Search size={16} className="text-gray-400 shrink-0" />
            <input
              type="text"
              value={eventTypeFilter}
              onChange={(e) => {
                setEventTypeFilter(e.target.value);
                setPage(1);
              }}
              placeholder="Filter by event type (e.g. ReleaseCreated)"
              className="flex-1 text-sm focus:outline-none"
            />
            <Button variant="secondary" onClick={() => refetch()} loading={isFetching}>
              <RefreshCw size={14} />
              Refresh
            </Button>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shield size={16} className="text-gray-500" />
              <h2 className="font-semibold text-gray-800">Audit Events</h2>
            </div>
            {data && (
              <span className="text-sm text-gray-500">{data.totalCount} total</span>
            )}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-gray-400" />
            </div>
          ) : isError ? (
            <p className="px-6 py-12 text-sm text-red-500 text-center">
              Failed to load audit events
            </p>
          ) : !data?.items?.length ? (
            <p className="px-6 py-12 text-sm text-gray-400 text-center">
              No audit events found
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50 text-left">
                  <th className="px-6 py-3 font-medium text-gray-500">Event Type</th>
                  <th className="px-6 py-3 font-medium text-gray-500">Actor</th>
                  <th className="px-6 py-3 font-medium text-gray-500">Aggregate</th>
                  <th className="px-6 py-3 font-medium text-gray-500">Timestamp</th>
                  <th className="px-6 py-3 font-medium text-gray-500">Hash</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data.items.map((e) => (
                  <tr key={e.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 font-medium text-gray-800">{e.eventType}</td>
                    <td className="px-6 py-3 text-gray-600">{e.actorEmail}</td>
                    <td className="px-6 py-3 text-gray-600">{e.aggregateType}</td>
                    <td className="px-6 py-3 text-xs text-gray-500">
                      {new Date(e.occurredAt).toLocaleString()}
                    </td>
                    <td
                      className="px-6 py-3 font-mono text-xs text-gray-400 truncate max-w-[120px]"
                      title={e.hash}
                      aria-label={`Hash: ${e.hash}`}
                    >
                      {e.hash.slice(0, 12)}…
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-6 py-4 flex items-center justify-between border-t border-gray-100">
            <Button
              variant="secondary"
              disabled={page === 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Previous
            </Button>
            <span className="text-sm text-gray-500">
              Page {data.page} of {data.totalPages}
            </span>
            <Button
              variant="secondary"
              disabled={page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </Card>
    </div>
  );
}
