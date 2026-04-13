// import { useTranslation } from 'react-i18next';
// import { LogOut } from 'lucide-react';
// import { cn } from '../../lib/cn';

// interface AppSidebarFooterProps {
//   collapsed?: boolean;
//   email?: string;
//   persona?: string;
//   roleName?: string;
//   onLogout: () => void;
// }

// /**
//  * AppSidebarFooter — secção do utilizador no rodapé da sidebar.
//  *
//  * Collapsed: avatar com initial (gradiente de marca) + ação de logout ao hover.
//  * Expanded: avatar, display name, persona/role, botão de logout ao hover.
//  */
// export function AppSidebarFooter({ collapsed = false, email, persona, roleName, onLogout }: AppSidebarFooterProps) {
//   const { t } = useTranslation();
//   const initial = email?.[0]?.toUpperCase() ?? 'U';
//   const displayName = email?.split('@')[0] ?? t('common.user');

//   return (
//     <div className={cn('border-t border-edge shrink-0', collapsed ? 'p-2 flex justify-center' : 'p-3.5')}>
//       {collapsed ? (
//         <button
//           onClick={onLogout}
//           className="w-10 h-10 rounded-xl flex items-center justify-center text-white text-sm font-bold hover:brightness-90 transition-all duration-[var(--nto-motion-base)]"
//           style={{ background: 'var(--nto-gradient-logo)' }}
//           title={t('auth.signOut')}
//           aria-label={t('auth.signOut')}
//         >
//           {initial}
//         </button>
//       ) : (
//         <div className="flex items-center gap-3 rounded-xl px-2.5 py-2 hover:bg-hover transition-colors group cursor-default">
//           {/* Avatar com gradiente da marca */}
//           <div
//             className="w-8 h-8 rounded-xl flex items-center justify-center text-xs font-bold shrink-0 text-white"
//             style={{ background: 'var(--nto-gradient-logo)' }}
//             aria-hidden="true"
//           >
//             {initial}
//           </div>

//           {/* User info */}
//           <div className="flex-1 min-w-0">
//             <p className="text-sm font-semibold text-heading truncate leading-tight">{displayName}</p>
//             <p className="text-[10px] text-muted truncate leading-tight">
//               {persona ? t(`persona.${persona}.label`) : ''}
//               {roleName ? ` · ${roleName}` : ''}
//             </p>
//           </div>

//           {/* Logout — visível ao hover do grupo */}
//           <button
//             onClick={onLogout}
//             className="p-1.5 rounded-lg text-faded hover:text-critical opacity-0 group-hover:opacity-100 transition-all duration-[var(--nto-motion-fast)] hover:bg-critical/10 shrink-0"
//             title={t('auth.signOut')}
//             aria-label={t('auth.signOut')}
//           >
//             <LogOut size={14} />
//           </button>
//         </div>
//       )}
//     </div>
//   );
// }
