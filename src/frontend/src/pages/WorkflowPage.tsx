import { CheckSquare, Clock, Info } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../components/Card';
import { Badge } from '../components/Badge';

export function WorkflowPage() {
  return (
    <div className="p-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Workflow & Approvals</h1>
        <p className="text-gray-500 mt-1">Manage approval workflows and evidence packs</p>
      </div>

      {/* Coming soon banner */}
      <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 flex items-start gap-3">
        <Info size={18} className="text-blue-600 mt-0.5 shrink-0" />
        <div>
          <p className="text-sm font-medium text-blue-800">Workflow endpoints are being finalized</p>
          <p className="text-sm text-blue-600 mt-0.5">
            The backend workflow module is scaffolded. Full UI will be available once endpoint implementation is complete.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Clock size={16} className="text-gray-500" />
              <h2 className="font-semibold text-gray-800">Pending Approvals</h2>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-gray-400 text-center py-8">
              No pending approvals
            </p>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <CheckSquare size={16} className="text-gray-500" />
              <h2 className="font-semibold text-gray-800">Workflow Templates</h2>
            </div>
          </CardHeader>
          <CardBody>
            <ul className="space-y-3">
              {['Standard Release', 'Breaking Change Release', 'Hotfix Release'].map((name, i) => (
                <li key={name} className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
                  <span className="text-sm text-gray-700">{name}</span>
                  <Badge variant={i === 1 ? 'danger' : i === 2 ? 'warning' : 'info'}>
                    Level {i + 1}
                  </Badge>
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
