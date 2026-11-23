import { lazy } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { AdminLayout } from "../layouts/AdminLayout";
import { RequireAuth, RequirePermission } from "../guards/RouteGuards";
import { PERMISSIONS, OWNER_PERMISSIONS } from "../../shared/constants/permissions";

/** Lazy yüklenen feature modülleri (doküman 26.1 code splitting). */
const LoginPage = lazy(() => import("../../features/auth/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const DashboardPage = lazy(() => import("../../features/dashboard/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const OwnerDashboardPage = lazy(() => import("../../features/owner/pages/OwnerDashboardPage").then((m) => ({ default: m.OwnerDashboardPage })));
const OwnerBusinessPage = lazy(() => import("../../features/owner/pages/OwnerBusinessPage").then((m) => ({ default: m.OwnerBusinessPage })));
const OwnerServicesPage = lazy(() => import("../../features/owner/pages/OwnerServicesPage").then((m) => ({ default: m.OwnerServicesPage })));
const OwnerEmployeesPage = lazy(() => import("../../features/owner/pages/OwnerEmployeesPage").then((m) => ({ default: m.OwnerEmployeesPage })));
const OwnerAppointmentsPage = lazy(() => import("../../features/owner/pages/OwnerAppointmentsPage").then((m) => ({ default: m.OwnerAppointmentsPage })));
const OwnerFinancePage = lazy(() => import("../../features/owner/pages/OwnerFinancePage").then((m) => ({ default: m.OwnerFinancePage })));
const OwnerStockPage = lazy(() => import("../../features/owner/pages/OwnerStockPage").then((m) => ({ default: m.OwnerStockPage })));
const OwnerReviewsPage = lazy(() => import("../../features/owner/pages/OwnerReviewsPage").then((m) => ({ default: m.OwnerReviewsPage })));
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
      { path: "owner", element: <RequirePermission permission={OWNER_PERMISSIONS.dashboard}><OwnerDashboardPage /></RequirePermission> },
      { path: "owner/business", element: <RequirePermission permission={OWNER_PERMISSIONS.business}><OwnerBusinessPage /></RequirePermission> },
      { path: "owner/services", element: <RequirePermission permission={OWNER_PERMISSIONS.services}><OwnerServicesPage /></RequirePermission> },
      { path: "owner/employees", element: <RequirePermission permission={OWNER_PERMISSIONS.employees}><OwnerEmployeesPage /></RequirePermission> },
      { path: "owner/appointments", element: <RequirePermission permission={OWNER_PERMISSIONS.appointments}><OwnerAppointmentsPage /></RequirePermission> },
      { path: "owner/finance", element: <RequirePermission permission={OWNER_PERMISSIONS.finance}><OwnerFinancePage /></RequirePermission> },
      { path: "owner/stock", element: <RequirePermission permission={OWNER_PERMISSIONS.stock}><OwnerStockPage /></RequirePermission> },
      { path: "owner/reviews", element: <RequirePermission permission={OWNER_PERMISSIONS.reviews}><OwnerReviewsPage /></RequirePermission> },
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
