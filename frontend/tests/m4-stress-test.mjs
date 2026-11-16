/**
 * SIGA - Milestone 4 Adversarial Stress Test Suite
 * Tests Route Guards, Permission Evaluation, Query Params, Re-entrancy, and Navigation.
 */

// --- 1. Polyfill Browser Globals for Node.js Execution ---
const hashListeners = [];
let currentHash = '';

const fakeStorage = new Map();
const fakeLocalStorage = {
  getItem: (k) => (fakeStorage.has(k) ? fakeStorage.get(k) : null),
  setItem: (k, v) => fakeStorage.set(k, String(v)),
  removeItem: (k) => fakeStorage.delete(k),
  clear: () => fakeStorage.clear()
};

const fakeSessionStorage = {
  getItem: (k) => (fakeStorage.has('sess_' + k) ? fakeStorage.get('sess_' + k) : null),
  setItem: (k, v) => fakeStorage.set('sess_' + k, String(v)),
  removeItem: (k) => fakeStorage.delete('sess_' + k),
  clear: () => fakeStorage.clear()
};

globalThis.localStorage = fakeLocalStorage;
globalThis.sessionStorage = fakeSessionStorage;

function createMockElement(tag = 'div') {
  const classes = new Set();
  const attrs = new Map();
  const children = [];
  return {
    tagName: tag.toUpperCase(),
    className: '',
    id: '',
    style: {},
    classList: {
      add: (...c) => c.forEach(x => classes.add(x)),
      remove: (...c) => c.forEach(x => classes.delete(x)),
      toggle: (c, force) => {
        if (force === undefined) {
          if (classes.has(c)) classes.delete(c); else classes.add(c);
        } else if (force) {
          classes.add(c);
        } else {
          classes.delete(c);
        }
      },
      contains: (c) => classes.has(c)
    },
    setAttribute: (k, v) => attrs.set(k, String(v)),
    getAttribute: (k) => attrs.get(k) || null,
    hasAttribute: (k) => attrs.has(k),
    removeAttribute: (k) => attrs.delete(k),
    appendChild: (ch) => children.push(ch),
    remove: () => {},
    addEventListener: () => {},
    querySelector: () => null,
    querySelectorAll: () => [],
    innerHTML: ''
  };
}

globalThis.document = {
  title: '',
  getElementById: (id) => null,
  createElement: (tag) => createMockElement(tag),
  body: createMockElement('body'),
  addEventListener: () => {}
};

globalThis.window = {
  location: {
    hostname: 'localhost',
    protocol: 'http:',
    get hash() {
      return currentHash;
    },
    set hash(val) {
      currentHash = val;
      // Trigger listeners
      hashListeners.forEach(fn => fn({ type: 'hashchange', newURL: val }));
    }
  },
  addEventListener: (evt, fn) => {
    if (evt === 'hashchange') hashListeners.push(fn);
  },
  removeEventListener: (evt, fn) => {
    if (evt === 'hashchange') {
      const idx = hashListeners.indexOf(fn);
      if (idx !== -1) hashListeners.splice(idx, 1);
    }
  },
  innerWidth: 1200
};

globalThis.atob = (str) => Buffer.from(str, 'base64').toString('binary');
globalThis.btoa = (str) => Buffer.from(str, 'binary').toString('base64');

// --- 2. Dynamic Import of Code Under Test ---
const { Router } = await import('../js/router.js');
const { authService } = await import('../js/services/auth-service.js');
const { storage } = await import('../js/utils/storage.js');
const { getProhibitionIcon } = await import('../js/utils/icons.js');

// --- 3. Test Runner & Assertion Helpers ---
let totalTests = 0;
let passedTests = 0;
let failedTests = 0;
const failures = [];

function assert(condition, message) {
  totalTests++;
  if (condition) {
    passedTests++;
    console.log(`  ✓ PASS: ${message}`);
  } else {
    failedTests++;
    failures.push(message);
    console.error(`  ✗ FAIL: ${message}`);
  }
}

function assertEqual(actual, expected, message) {
  assert(actual === expected, `${message} (Expected: ${JSON.stringify(expected)}, Actual: ${JSON.stringify(actual)})`);
}

function assertDeepEqual(actual, expected, message) {
  assert(JSON.stringify(actual) === JSON.stringify(expected), `${message} (Expected: ${JSON.stringify(expected)}, Actual: ${JSON.stringify(actual)})`);
}

console.log('================================================================');
console.log('SIGA MILESTONE 4: ADVERSARIAL EMPIRICAL STRESS TEST SUITE');
console.log('================================================================\n');

// ----------------------------------------------------------------
// SUITE 1: Permission Evaluation Stress Testing
// ----------------------------------------------------------------
console.log('--- SUITE 1: Permission Evaluation Stress Testing ---');

// Helper to set session state
function setTestSession(user, token = 'dummy.mock.token') {
  storage.clearSession();
  authService._permisosSet = null;
  if (user) {
    storage.setUser(user, true);
    storage.setToken(token, true);
    if (user.permisos) {
      authService._guardarPermisosEnMemoria(user.permisos);
    }
  }
}

// 1.1 Unauthenticated permission tests
setTestSession(null);
assertEqual(authService.estaAutenticado(), false, '1.1.1 Session should be unauthenticated');
assertEqual(authService.tienePermiso('ROLES_VER'), false, '1.1.2 Unauthenticated tienePermiso("ROLES_VER") returns false');
assertEqual(authService.tienePermiso(null), false, '1.1.3 Unauthenticated tienePermiso(null) returns false');
assertEqual(authService.tienePermiso(undefined), false, '1.1.4 Unauthenticated tienePermiso(undefined) returns false');
assertEqual(authService.tienePermiso(''), false, '1.1.5 Unauthenticated tienePermiso("") returns false');
assertEqual(authService.tieneAlgunPermiso(['ROLES_VER']), false, '1.1.6 Unauthenticated tieneAlgunPermiso returns false');
assertEqual(authService.tieneTodosLosPermisos(['ROLES_VER']), false, '1.1.7 Unauthenticated tieneTodosLosPermisos returns false');

// 1.2 Authenticated non-admin with specific permissions
const testUser = {
  id: 10,
  username: 'operador1',
  rol: 'OPERADOR',
  roles: ['OPERADOR'],
  permisos: ['CLIENTES_VER', 'STOCK_VER', 'MOVCOM_VER']
};
setTestSession(testUser);

assertEqual(authService.estaAutenticado(), true, '1.2.1 Session is authenticated');
assertEqual(authService.tienePermiso(null), true, '1.2.2 Authenticated tienePermiso(null) returns true (no guard)');
assertEqual(authService.tienePermiso(undefined), true, '1.2.3 Authenticated tienePermiso(undefined) returns true (no guard)');
assertEqual(authService.tienePermiso(''), true, '1.2.4 Authenticated tienePermiso("") returns true (empty code)');
assertEqual(authService.tienePermiso('   '), true, '1.2.5 Authenticated tienePermiso("   ") returns true (whitespace)');

// Case insensitivity & trimming
assertEqual(authService.tienePermiso('CLIENTES_VER'), true, '1.2.6 tienePermiso exact uppercase matches');
assertEqual(authService.tienePermiso('clientes_ver'), true, '1.2.7 tienePermiso lowercase normalizes to uppercase');
assertEqual(authService.tienePermiso('  Clientes_Ver  '), true, '1.2.8 tienePermiso mixed case with whitespace normalizes');
assertEqual(authService.tienePermiso('ROLES_VER'), false, '1.2.9 tienePermiso for unassigned permission returns false');
assertEqual(authService.tienePermiso('roles_ver'), false, '1.2.10 tienePermiso lowercase for unassigned returns false');

// Non-string inputs
assertEqual(authService.tienePermiso(123), true, '1.2.11 tienePermiso non-string number treated as non-guarded (true)');
assertEqual(authService.tienePermiso({}), true, '1.2.12 tienePermiso non-string object treated as non-guarded (true)');

// 1.3 Logical OR: tieneAlgunPermiso
assertEqual(authService.tieneAlgunPermiso([]), true, '1.3.1 tieneAlgunPermiso([]) empty array returns true');
assertEqual(authService.tieneAlgunPermiso(null), true, '1.3.2 tieneAlgunPermiso(null) non-array returns true');
assertEqual(authService.tieneAlgunPermiso(['ROLES_VER', 'CLIENTES_VER']), true, '1.3.3 tieneAlgunPermiso with 1 match returns true');
assertEqual(authService.tieneAlgunPermiso(['roles_ver', 'stock_ver']), true, '1.3.4 tieneAlgunPermiso case-insensitive match returns true');
assertEqual(authService.tieneAlgunPermiso(['ROLES_VER', 'EMPLEADOS_VER']), false, '1.3.5 tieneAlgunPermiso with 0 matches returns false');

// 1.4 Logical AND: tieneTodosLosPermisos
assertEqual(authService.tieneTodosLosPermisos([]), true, '1.4.1 tieneTodosLosPermisos([]) empty array returns true');
assertEqual(authService.tieneTodosLosPermisos(null), true, '1.4.2 tieneTodosLosPermisos(null) non-array returns true');
assertEqual(authService.tieneTodosLosPermisos(['CLIENTES_VER', 'STOCK_VER']), true, '1.4.3 tieneTodosLosPermisos with all matching returns true');
assertEqual(authService.tieneTodosLosPermisos(['CLIENTES_VER', 'ROLES_VER']), false, '1.4.4 tieneTodosLosPermisos partial match returns false');
assertEqual(authService.tieneTodosLosPermisos(['ROLES_VER', 'EMPLEADOS_VER']), false, '1.4.5 tieneTodosLosPermisos no matches returns false');

// 1.5 Superadmin user permissions bypass
const superAdminUser = {
  id: 1,
  username: 'admin',
  rol: 'ADMINISTRADOR',
  roles: ['ADMINISTRADOR'],
  permisos: [] // Even with empty permissions array, superadmin bypass grants all
};
setTestSession(superAdminUser);
assertEqual(authService.tienePermiso('ROLES_VER'), true, '1.5.1 Superadmin tienePermiso("ROLES_VER") returns true');
assertEqual(authService.tienePermiso('NON_EXISTENT_PERMISSION_XYZ'), true, '1.5.2 Superadmin has unlisted permissions');
assertEqual(authService.tieneAlgunPermiso(['NON_EXISTENT_1', 'NON_EXISTENT_2']), true, '1.5.3 Superadmin tieneAlgunPermiso bypass');
assertEqual(authService.tieneTodosLosPermisos(['NON_EXISTENT_1', 'NON_EXISTENT_2']), true, '1.5.4 Superadmin tieneTodosLosPermisos bypass');

// TitleCase "Administrador"
const superAdminTitleCase = {
  id: 2,
  username: 'admin2',
  rol: 'Administrador',
  roles: ['Administrador'],
  permisos: []
};
setTestSession(superAdminTitleCase);
assertEqual(authService.tienePermiso('ROLES_VER'), true, '1.5.5 Superadmin TitleCase "Administrador" bypass');


// ----------------------------------------------------------------
// SUITE 2: Hash Parsing and Query String Decomposition Stress Testing
// ----------------------------------------------------------------
console.log('\n--- SUITE 2: Hash Parsing and Query String Stress Testing ---');

const router = new Router();

const parsed1 = router._parseHash('#roles?id=123&action=edit');
assertEqual(parsed1.path, '#roles', '2.1.1 _parseHash strips query parameters to extract route path');
assertDeepEqual(parsed1.params, { id: '123', action: 'edit' }, '2.1.2 _parseHash correctly parses query parameters into key-values');

const parsed2 = router._parseHash('###roles?filter=active');
assertEqual(parsed2.path, '#roles', '2.2.1 _parseHash handles multiple leading hashes');
assertDeepEqual(parsed2.params, { filter: 'active' }, '2.2.2 _parseHash parses params with multiple hashes');

const parsed3 = router._parseHash('#dashboard');
assertEqual(parsed3.path, '#dashboard', '2.3.1 _parseHash with no query parameters');
assertDeepEqual(parsed3.params, {}, '2.3.2 _parseHash params empty object when no query');

const parsed4 = router._parseHash('#roles?query=val1?extra=val2');
assertEqual(parsed4.path, '#roles', '2.4.1 _parseHash handles multiple question marks safely');

const parsed5 = router._parseHash('');
assertEqual(parsed5.path, '#', '2.5.1 _parseHash handles empty hash');

const parsed6 = router._parseHash('#roles?search=Juan%20Perez&order=desc');
assertEqual(parsed6.params.search, 'Juan Perez', '2.6.1 _parseHash decodes URI components cleanly');


// ----------------------------------------------------------------
// SUITE 3: Adversarial Route Guard & Navigation Lifecycle Testing
// ----------------------------------------------------------------
console.log('\n--- SUITE 3: Adversarial Route Guard & Navigation Testing ---');

// Mock views
const mockLoginView = { render: (el, params) => {} };
const mockDashboardView = { render: (el, params) => {} };
const mockPerfilView = { render: (el, params) => {} };
const mockRolesView = { render: (el, params) => {} };
const mockEmpleadosView = { render: (el, params) => {} };
const mockStockView = { render: (el, params) => {} };

// Setup fresh router with registered routes
const testRouter = new Router();
testRouter.register('#login', mockLoginView, false, 'Iniciar Sesion', null);
testRouter.register('#dashboard', mockDashboardView, true, 'Dashboard', null);
testRouter.register('#perfil', mockPerfilView, true, 'Perfil', null);
testRouter.register('#roles', mockRolesView, true, 'Roles', 'ROLES_VER');
testRouter.register('#empleados', mockEmpleadosView, true, 'Empleados', 'EMPLEADOS_VER');
testRouter.register('#stock', mockStockView, true, 'Stock', 'STOCK_VER');

let lastRouteNotified = null;
testRouter.setOnRouteChange((info) => {
  lastRouteNotified = info;
});

// 3.1 Unauthenticated navigation attempts
setTestSession(null);

window.location.hash = '#perfil';
// Trigger routing manually to verify synchronous execution flow
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#login', '3.1.1 Unauthenticated entering #perfil redirects to #login');
assertEqual(testRouter._isNavigating, false, '3.1.2 _isNavigating is unlocked after unauthenticated redirect');

window.location.hash = '#roles';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#login', '3.1.3 Unauthenticated entering #roles redirects to #login');
assertEqual(testRouter._isNavigating, false, '3.1.4 _isNavigating is unlocked');

window.location.hash = '#empleados';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#login', '3.1.5 Unauthenticated entering #empleados redirects to #login');
assertEqual(testRouter._isNavigating, false, '3.1.6 _isNavigating is unlocked');

// 3.2 Authenticated user lacking ROLES_VER
const regularUser = {
  id: 5,
  username: 'vendedor',
  rol: 'VENDEDOR',
  roles: ['VENDEDOR'],
  permisos: ['STOCK_VER']
};
setTestSession(regularUser);

window.location.hash = '#roles';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#dashboard', '3.2.1 User lacking ROLES_VER entering #roles redirects to #dashboard');
assertEqual(testRouter._isNavigating, false, '3.2.2 _isNavigating is unlocked after unauthorized redirect');

// Check with query parameters: #roles?id=123&action=edit
window.location.hash = '#roles?id=123&action=edit';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#dashboard', '3.2.3 User lacking ROLES_VER entering #roles with query params still redirected to #dashboard');
assertEqual(testRouter._isNavigating, false, '3.2.4 _isNavigating is unlocked');

// 3.3 Authenticated user with ROLES_VER enters #roles
const adminRolesUser = {
  id: 6,
  username: 'adminroles',
  rol: 'AUDITOR',
  roles: ['AUDITOR'],
  permisos: ['ROLES_VER', 'STOCK_VER']
};
setTestSession(adminRolesUser);

window.location.hash = '#roles';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#roles', '3.3.1 User with ROLES_VER successfully accesses #roles');
assertEqual(testRouter.currentRoute, '#roles', '3.3.2 testRouter.currentRoute updated to #roles');
assertEqual(testRouter._isNavigating, false, '3.3.3 _isNavigating reset to false');
assertEqual(lastRouteNotified.path, '#roles', '3.3.4 onRouteChange received #roles notification');

// 3.4 Authenticated user with ROLES_VER enters #roles with query params
window.location.hash = '#roles?id=42&mode=edit';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#roles?id=42&mode=edit', '3.4.1 Route stays on #roles?id=42&mode=edit');
assertEqual(testRouter.currentRoute, '#roles', '3.4.2 Current route is #roles');
assertEqual(lastRouteNotified.params.id, '42', '3.4.3 Query parameter "id" passed to view');
assertEqual(lastRouteNotified.params.mode, 'edit', '3.4.4 Query parameter "mode" passed to view');

// 3.5 Re-entrancy & Sequential Navigation Stress Test
// Rapid unauthorized jump followed immediately by authorized jump
setTestSession(regularUser); // only STOCK_VER
window.location.hash = '#roles'; // Should redirect to #dashboard
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#dashboard', '3.5.1 Rapid sequence: first jump redirected to #dashboard');

// Immediately follow with #stock
window.location.hash = '#stock';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#stock', '3.5.2 Rapid sequence: second jump immediately renders #stock without deadlock');
assertEqual(testRouter.currentRoute, '#stock', '3.5.3 currentRoute updated to #stock');

// 3.6 Unregistered Route Handling
window.location.hash = '#non-existent-route-999';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#dashboard', '3.6.1 Unregistered route for authenticated user redirects cleanly to #dashboard');

setTestSession(null);
window.location.hash = '#non-existent-route-999';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#login', '3.6.2 Unregistered route for unauthenticated user redirects cleanly to #login');

// 3.7 Authenticated user visiting #login
setTestSession(regularUser);
window.location.hash = '#login';
await testRouter._handleHashChange();
assertEqual(window.location.hash, '#dashboard', '3.7.1 Authenticated user entering #login is redirected to #dashboard');


// ----------------------------------------------------------------
// SUITE 4: Section 5 ("Administración") Visibility Stress Testing
// ----------------------------------------------------------------
console.log('\n--- SUITE 4: Section 5 ("Administracion") Visibility Stress Testing ---');

// Simulate the DOM structure of app.js sidebar
function buildMockSidebar() {
  const root = createMockElement('div');
  const nav = createMockElement('nav');
  nav.className = 'sidebar-nav';

  // Section 5
  const section5 = createMockElement('div');
  section5.className = 'nav-section';
  section5.setAttribute('data-section', 'administracion');

  const content = createMockElement('div');
  content.className = 'nav-section-content';

  const items = [
    { path: '#clientes', perm: 'CLIENTES_VER' },
    { path: '#sucursales', perm: 'SUCURSALES_VER' },
    { path: '#empleados', perm: 'EMPLEADOS_VER' },
    { path: '#roles', perm: 'ROLES_VER' },
    { path: '#precios-referencia', perm: 'PRECIOS_REF_VER' }
  ];

  const itemElements = [];
  items.forEach(it => {
    const a = createMockElement('a');
    a.className = 'nav-item';
    a.setAttribute('data-path', it.path);
    a.setAttribute('data-permiso', it.perm);
    content.appendChild(a);
    itemElements.push(a);
  });

  section5.appendChild(content);
  nav.appendChild(section5);
  root.appendChild(nav);

  return { root, nav, section5, content, itemElements };
}

// Mimic _applyNavPermissionFiltering logic from app.js
function applyNavPermissionFiltering(sidebarData) {
  const { section5, itemElements } = sidebarData;

  // Step 1: filter items
  itemElements.forEach(item => {
    const perm = item.getAttribute('data-permiso');
    const allowed = authService.tienePermiso(perm);
    item.classList.toggle('hidden', !allowed);
  });

  // Step 2: filter section
  const visibleItems = itemElements.filter(it => !it.classList.contains('hidden'));
  if (visibleItems.length === 0) {
    section5.classList.add('hidden');
  } else {
    section5.classList.remove('hidden');
  }
}

// 4.1 User with NO administrative permissions
const nonAdminUser = {
  id: 20,
  username: 'campo1',
  rol: 'PEON',
  roles: ['PEON'],
  permisos: ['TEMPORADAS_VER', 'PLANTACIONES_VER']
};
setTestSession(nonAdminUser);

const sidebar1 = buildMockSidebar();
applyNavPermissionFiltering(sidebar1);

sidebar1.itemElements.forEach((item, idx) => {
  assertEqual(item.classList.contains('hidden'), true, `4.1.${idx + 1} Admin nav-item ${item.getAttribute('data-path')} is hidden for non-admin`);
});
assertEqual(sidebar1.section5.classList.contains('hidden'), true, '4.1.6 Entire Section 5 is completely hidden when user has 0 admin permissions');

// 4.2 User with ONLY 1 administrative permission (e.g. CLIENTES_VER)
const clientManagerUser = {
  id: 21,
  username: 'recepcion',
  rol: 'RECEPCION',
  roles: ['RECEPCION'],
  permisos: ['CLIENTES_VER']
};
setTestSession(clientManagerUser);

const sidebar2 = buildMockSidebar();
applyNavPermissionFiltering(sidebar2);

assertEqual(sidebar2.itemElements[0].classList.contains('hidden'), false, '4.2.1 #clientes is NOT hidden for user with CLIENTES_VER');
assertEqual(sidebar2.itemElements[1].classList.contains('hidden'), true, '4.2.2 #sucursales is hidden');
assertEqual(sidebar2.itemElements[2].classList.contains('hidden'), true, '4.2.3 #empleados is hidden');
assertEqual(sidebar2.itemElements[3].classList.contains('hidden'), true, '4.2.4 #roles is hidden');
assertEqual(sidebar2.itemElements[4].classList.contains('hidden'), true, '4.2.5 #precios-referencia is hidden');
assertEqual(sidebar2.section5.classList.contains('hidden'), false, '4.2.6 Section 5 remains visible because it has 1 visible item');

// 4.3 Superadmin user
setTestSession(superAdminUser);
const sidebar3 = buildMockSidebar();
applyNavPermissionFiltering(sidebar3);

sidebar3.itemElements.forEach((item, idx) => {
  assertEqual(item.classList.contains('hidden'), false, `4.3.${idx + 1} Admin nav-item ${item.getAttribute('data-path')} is visible for superadmin`);
});
assertEqual(sidebar3.section5.classList.contains('hidden'), false, '4.3.6 Section 5 is visible for superadmin');


// ----------------------------------------------------------------
// SUITE 5: Prohibition Icon & Trash Can Elimination Forensics
// ----------------------------------------------------------------
console.log('\n--- SUITE 5: Prohibition Icon Forensics ---');

const iconHtml = getProhibitionIcon({ size: 'sm', title: 'Dar de baja' });
assert(iconHtml.includes('<circle cx="12" cy="12" r="10"></circle>'), '5.1.1 Prohibition icon contains outer circle (r=10)');
assert(iconHtml.includes('<line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line>'), '5.1.2 Prohibition icon contains exact diagonal slash (45-degree angle)');
assert(iconHtml.includes('style="color: var(--danger);"'), '5.1.3 Prohibition icon applies var(--danger) color styling');
assert(iconHtml.includes('role="img"'), '5.1.4 Prohibition icon has accessibility role="img"');
assert(iconHtml.includes('aria-label="Dar de baja"'), '5.1.5 Prohibition icon has accessibility aria-label');


// ----------------------------------------------------------------
// FINAL RESULTS SUMMARY
// ----------------------------------------------------------------
console.log('\n================================================================');
console.log(`TOTAL TESTS EXECUTED : ${totalTests}`);
console.log(`PASSED               : ${passedTests}`);
console.log(`FAILED               : ${failedTests}`);
console.log('================================================================\n');

if (failedTests > 0) {
  console.error('FAILURES ENCOUNTERED:');
  failures.forEach((f, idx) => console.error(`  ${idx + 1}. ${f}`));
  process.exit(1);
} else {
  console.log('ALL EMPIRICAL TESTS PASSED CONVINCINGLY (0 FAILURES).');
  process.exit(0);
}
