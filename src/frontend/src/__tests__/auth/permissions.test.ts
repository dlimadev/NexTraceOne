import { describe, it, expect } from 'vitest';
import { getPermissionsForRoles, hasPermission } from '../../auth/permissions';

describe('getPermissionsForRoles', () => {
  it('retorna conjunto vazio para roles inválidas', () => {
    const perms = getPermissionsForRoles(['InvalidRole', 'Unknown']);
    expect(perms.size).toBe(0);
  });

  it('Admin possui todas as permissões', () => {
    const perms = getPermissionsForRoles(['Admin']);
    expect(perms.has('users:read')).toBe(true);
    expect(perms.has('users:write')).toBe(true);
    expect(perms.has('audit:read')).toBe(true);
    expect(perms.has('audit:export')).toBe(true);
    expect(perms.has('workflow:approve')).toBe(true);
  });

  it('Developer não possui permissão de users:read', () => {
    const perms = getPermissionsForRoles(['Developer']);
    expect(perms.has('users:read')).toBe(false);
    expect(perms.has('users:write')).toBe(false);
  });

  it('Developer pode ler e escrever releases e contratos', () => {
    const perms = getPermissionsForRoles(['Developer']);
    expect(perms.has('releases:read')).toBe(true);
    expect(perms.has('releases:write')).toBe(true);
    expect(perms.has('contracts:read')).toBe(true);
    expect(perms.has('contracts:write')).toBe(true);
  });

  it('Viewer possui apenas permissões de leitura', () => {
    const perms = getPermissionsForRoles(['Viewer']);
    expect(perms.has('releases:read')).toBe(true);
    expect(perms.has('releases:write')).toBe(false);
    expect(perms.has('users:read')).toBe(false);
    expect(perms.has('workflow:approve')).toBe(false);
  });

  it('Auditor pode ler e exportar audit, mas não modificar releases', () => {
    const perms = getPermissionsForRoles(['Auditor']);
    expect(perms.has('audit:read')).toBe(true);
    expect(perms.has('audit:export')).toBe(true);
    expect(perms.has('releases:write')).toBe(false);
    expect(perms.has('users:write')).toBe(false);
  });

  it('Manager pode aprovar workflows mas não users:write', () => {
    const perms = getPermissionsForRoles(['Manager']);
    expect(perms.has('workflow:approve')).toBe(true);
    expect(perms.has('users:read')).toBe(true);
    expect(perms.has('users:write')).toBe(false);
  });

  it('múltiplos roles fazem union de permissões', () => {
    const perms = getPermissionsForRoles(['Developer', 'Auditor']);
    // Developer pode escrever releases, Auditor pode exportar audit
    expect(perms.has('releases:write')).toBe(true);
    expect(perms.has('audit:export')).toBe(true);
  });

  it('lista de roles vazia retorna conjunto vazio', () => {
    const perms = getPermissionsForRoles([]);
    expect(perms.size).toBe(0);
  });
});

describe('hasPermission', () => {
  it('retorna true quando a role possui a permissão', () => {
    expect(hasPermission(['Admin'], 'users:write')).toBe(true);
    expect(hasPermission(['Developer'], 'releases:write')).toBe(true);
  });

  it('retorna false quando a role não possui a permissão', () => {
    expect(hasPermission(['Viewer'], 'releases:write')).toBe(false);
    expect(hasPermission(['Developer'], 'users:read')).toBe(false);
  });

  it('retorna false para roles inválidas', () => {
    expect(hasPermission(['UnknownRole'], 'releases:read')).toBe(false);
  });

  it('retorna false para lista de roles vazia', () => {
    expect(hasPermission([], 'releases:read')).toBe(false);
  });
});
