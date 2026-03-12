import { useQuery } from '@tanstack/react-query';
import { Zap, GitBranch, FileText, CheckSquare, Activity } from 'lucide-react';
import { StatCard } from '../components/StatCard';
import { Card, CardHeader, CardBody } from '../components/Card';
import { engineeringGraphApi } from '../api';

export function DashboardPage() {
  // Graph is used to get service/api counts
  const { data: graph } = useQuery({
    queryKey: ['graph'],
    queryFn: () => engineeringGraphApi.getGraph(),
    staleTime: 30_000,
  });

  const stats = [
    {
      title: 'Active Services',
      value: graph?.services?.length ?? '—',
      icon: <Activity size={28} />,
      color: 'text-indigo-600',
    },
    {
      title: 'Registered APIs',
      value: graph?.apis?.length ?? '—',
      icon: <GitBranch size={28} />,
      color: 'text-blue-600',
    },
    {
      title: 'Consumer Relations',
      value: graph?.relationships?.length ?? '—',
      icon: <Zap size={28} />,
      color: 'text-yellow-600',
    },
    {
      title: 'Total Contracts',
      value: '—',
      icon: <FileText size={28} />,
      color: 'text-green-600',
    },
    {
      title: 'Pending Approvals',
      value: '—',
      icon: <CheckSquare size={28} />,
      color: 'text-purple-600',
    },
  ];

  return (
    <div className="p-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-500 mt-1">Overview of your change intelligence platform</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
        {stats.map((s) => (
          <StatCard key={s.title} {...s} />
        ))}
      </div>

      {/* Services overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h2 className="text-base font-semibold text-gray-800">Recent Services</h2>
          </CardHeader>
          <CardBody className="p-0">
            {!graph?.services?.length ? (
              <p className="px-6 py-8 text-sm text-gray-400 text-center">
                No services registered yet
              </p>
            ) : (
              <ul className="divide-y divide-gray-100">
                {graph.services.slice(0, 5).map((svc) => (
                  <li key={svc.id} className="px-6 py-3 flex items-center gap-3">
                    <div className="w-8 h-8 rounded bg-indigo-100 flex items-center justify-center text-indigo-700 font-semibold text-sm">
                      {svc.name[0]}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-800">{svc.name}</p>
                      <p className="text-xs text-gray-400">{svc.team}</p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-base font-semibold text-gray-800">Registered APIs</h2>
          </CardHeader>
          <CardBody className="p-0">
            {!graph?.apis?.length ? (
              <p className="px-6 py-8 text-sm text-gray-400 text-center">
                No APIs registered yet
              </p>
            ) : (
              <ul className="divide-y divide-gray-100">
                {graph.apis.slice(0, 5).map((api) => (
                  <li key={api.id} className="px-6 py-3 flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-gray-800">{api.name}</p>
                      <p className="text-xs text-gray-400">{api.baseUrl}</p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
