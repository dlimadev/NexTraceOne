import { Shield, Info } from 'lucide-react';
import { Card, CardHeader } from '../components/Card';

const mockAuditEvents = [
  { id: '1', type: 'ReleaseCreated', actor: 'admin@acme.com', aggregate: 'Release', occurredAt: new Date().toISOString() },
  { id: '2', type: 'UserLoggedIn', actor: 'dev@acme.com', aggregate: 'Session', occurredAt: new Date(Date.now() - 60_000).toISOString() },
  { id: '3', type: 'ContractImported', actor: 'ci-pipeline@acme.com', aggregate: 'Contract', occurredAt: new Date(Date.now() - 120_000).toISOString() },
];

export function AuditPage() {
  return (
    <div className="p-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
        <p className="text-gray-500 mt-1">Immutable event trail with SHA-256 hash chain</p>
      </div>

      <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 flex items-start gap-3">
        <Info size={18} className="text-blue-600 mt-0.5 shrink-0" />
        <p className="text-sm text-blue-700">
          The audit module is scaffolded. Full search and integrity verification will be available once endpoint implementation is complete.
        </p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield size={16} className="text-gray-500" />
            <h2 className="font-semibold text-gray-800">Recent Events (Sample)</h2>
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 bg-gray-50 text-left">
                <th className="px-6 py-3 font-medium text-gray-500">Event Type</th>
                <th className="px-6 py-3 font-medium text-gray-500">Actor</th>
                <th className="px-6 py-3 font-medium text-gray-500">Aggregate</th>
                <th className="px-6 py-3 font-medium text-gray-500">Timestamp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {mockAuditEvents.map((e) => (
                <tr key={e.id} className="hover:bg-gray-50">
                  <td className="px-6 py-3 font-medium text-gray-800">{e.type}</td>
                  <td className="px-6 py-3 text-gray-600">{e.actor}</td>
                  <td className="px-6 py-3 text-gray-600">{e.aggregate}</td>
                  <td className="px-6 py-3 text-xs text-gray-500">
                    {new Date(e.occurredAt).toLocaleString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
