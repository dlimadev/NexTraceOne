import * as assert from 'assert';
import { parseCatalogResponse, buildServiceDashboardUrl } from '../utils';

describe('Utils', () => {
  describe('parseCatalogResponse', () => {
    it('parses array response', () => {
      const body = JSON.stringify([
        { name: 'payments-api', teamName: 'platform' },
        { name: 'orders-api' },
      ]);
      const result = parseCatalogResponse(body);
      assert.strictEqual(result.length, 2);
      assert.strictEqual(result[0].name, 'payments-api');
      assert.strictEqual(result[0].teamName, 'platform');
    });

    it('parses wrapped items response', () => {
      const body = JSON.stringify({
        items: [
          { name: 'payments-api', domain: 'finance' },
        ],
      });
      const result = parseCatalogResponse(body);
      assert.strictEqual(result.length, 1);
      assert.strictEqual(result[0].name, 'payments-api');
      assert.strictEqual(result[0].domain, 'finance');
    });

    it('returns empty array for empty wrapped response', () => {
      const body = JSON.stringify({ items: [] });
      const result = parseCatalogResponse(body);
      assert.strictEqual(result.length, 0);
    });

    it('throws on invalid JSON', () => {
      assert.throws(() => parseCatalogResponse('not-json'));
    });
  });

  describe('buildServiceDashboardUrl', () => {
    it('builds URL without trailing slash', () => {
      const url = buildServiceDashboardUrl('http://localhost:5000', 'payments-api');
      assert.strictEqual(url, 'http://localhost:5000/services/payments-api');
    });

    it('builds URL with trailing slash', () => {
      const url = buildServiceDashboardUrl('http://localhost:5000/', 'payments api');
      assert.strictEqual(url, 'http://localhost:5000/services/payments%20api');
    });
  });
});
