import { ArrowUpCircle, CheckCircle, XCircle, Info } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../components/Card';

const mockEnvironments = [
  { name: 'Development', color: 'bg-green-500' },
  { name: 'Staging', color: 'bg-yellow-500' },
  { name: 'Production', color: 'bg-red-500' },
];

const mockGates = [
  { name: 'Linting Passed', passed: true },
  { name: 'CI Tests Passed', passed: true },
  { name: 'Blast Radius Acceptable', passed: true },
  { name: 'Workflow Approved', passed: false },
  { name: 'Observability SLA Met', passed: true },
  { name: 'Budget Respected', passed: true },
];

export function PromotionPage() {
  return (
    <div className="p-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Promotion</h1>
        <p className="text-gray-500 mt-1">Control environment promotion with quality gates</p>
      </div>

      <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 flex items-start gap-3">
        <Info size={18} className="text-blue-600 mt-0.5 shrink-0" />
        <p className="text-sm text-blue-700">
          Promotion requests are created automatically after workflow approval. Manual promotion support is coming soon.
        </p>
      </div>

      {/* Environment pipeline */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="font-semibold text-gray-800">Environment Pipeline</h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-4">
            {mockEnvironments.map((env, i) => (
              <div key={env.name} className="flex items-center gap-4">
                <div className="flex flex-col items-center gap-2">
                  <div className={`w-3 h-3 rounded-full ${env.color}`} />
                  <span className="text-sm font-medium text-gray-700">{env.name}</span>
                </div>
                {i < mockEnvironments.length - 1 && (
                  <ArrowUpCircle size={20} className="text-gray-300 rotate-90" />
                )}
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Quality Gates */}
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-800">Quality Gates (Sample)</h2>
        </CardHeader>
        <CardBody className="p-0">
          <ul className="divide-y divide-gray-100">
            {mockGates.map((gate) => (
              <li key={gate.name} className="px-6 py-3 flex items-center justify-between">
                <span className="text-sm text-gray-700">{gate.name}</span>
                {gate.passed ? (
                  <div className="flex items-center gap-1 text-green-600 text-sm">
                    <CheckCircle size={14} /> <span>Passed</span>
                  </div>
                ) : (
                  <div className="flex items-center gap-1 text-red-500 text-sm">
                    <XCircle size={14} /> <span>Failed</span>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </CardBody>
      </Card>
    </div>
  );
}
