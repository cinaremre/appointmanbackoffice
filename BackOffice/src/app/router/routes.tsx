import { lazy } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { AdminLayout } from "../layouts/AdminLayout";
import { RequireAuth, RequirePermission } from "../guards/RouteGuards";
import { PERMISSIONS } from "../../shared/constants/permissions";

/** Lazy yuklenen feature modülleri (dokuman 26.1 code splitting). */
const LoginPage = lazy(() => import("../../features/auth/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const DashboardPage = lazy(() => import("../../features/dashboard/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const BusinessListPage = lazy(() => import("../../features/businesses/pages/BusinessListPage").then((m) => ({ default: m.BusinessListPage })));
const BusinessDetailPage = lazy(() => import("../../features/businesses/pages/BusinessDetailPage").then((m) => ({ default: m.BusinessDetailPage })));
const CategoriesPage = lazy(() => import("../../features/categories/pages/CategoriesPage").then((m) => ({ default: m.CategoriesPage })));
const ServicesPage = lazy(() => import("../../features/services/pages/ServicesPage").then((m) => ({ default: m.ServicesPage })));
const UserListPage = lazy(() => import("../../features/users/pages/UserListPage").then((m) => ({ default: m.UserListPage })));
const AppointmentsPage = lazy(() => import("../../features/appointments/pages/AppointmentsPage").then((m) => ({ default: m.AppointmentsPage })));
const SubscriptionsPage = lazy(() => import("../../features/subscriptions/pages/SubscriptionsPage").then((m) => ({ default: m.SubscriptionsPage })));
const FinancePage = lazy(() => import("../../features/finance/pages/FinancePage").then((m) => ({ default: m.FinancePage })));
const MarketingPage = lazy(() => import("../../features/marketing/pages/MarketingPage").then((m) => ({ default: m.MarketingPage })));
const OperationsPage = lazy(() => import("../../features/operations/pages/OperationsPage").then((m) => ({ default: m.OperationsPage })));
const AuditPage = lazy(() => import("../../features/audit/pages/AuditPage").then((m) => ({ default: m.AuditPage })));

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    path: "/",
    element: (
      <RequireAuth>
        <AdminLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "businesses", element: <RequirePermission permission={PERMISSIONS.businessRead}><BusinessListPage /></RequirePermission> },
      { path: "businesses/:id", element: <RequirePermission permission={PERMISSIONS.businessRead}><BusinessDetailPage /></RequirePermission> },
      { path: "categories", element: <RequirePermission permission={PERMISSIONS.categoryRead}><CategoriesPage /></RequirePermission> },
      { path: "services", element: <RequirePermission permission={PERMISSIONS.serviceRead}><ServicesPage /></RequirePermission> },
      { path: "users", element: <RequirePermission permission={PERMISSIONS.userRead}><UserListPage /></RequirePermission> },
      { path: "appointments", element: <RequirePermission permission={PERMISSIONS.appointmentRead}><AppointmentsPage /></RequirePermission> },
      { path: "subscriptions", element: <RequirePermission permission={PERMISSIONS.subscriptionRead}><SubscriptionsPage /></RequirePermission> },
      { path: "finance", element: <RequirePermission permission={PERMISSIONS.financeRead}><FinancePage /></RequirePermission> },
      { path: "marketing", element: <RequirePermission permission={PERMISSIONS.marketingRead}><MarketingPage /></RequirePermission> },
      { path: "operations", element: <RequirePermission permission={PERMISSIONS.operationsRead}><OperationsPage /></RequirePermission> },
      { path: "audit", element: <RequirePermission permission={PERMISSIONS.auditRead}><AuditPage /></RequirePermission> },
    ],
  },
  { path: "*", element: <Navigate to="/" replace /> },
]);
