import AccessibilityNewIcon from "@mui/icons-material/AccessibilityNew";
import AppRegistrationIcon from "@mui/icons-material/AppRegistration";
import AssessmentIcon from "@mui/icons-material/Assessment";
import AttachMoneyIcon from "@mui/icons-material/AttachMoney";
import BarChartIcon from "@mui/icons-material/BarChart";
import BusinessIcon from "@mui/icons-material/Business";
import CampaignIcon from "@mui/icons-material/Campaign";
import CategoryIcon from "@mui/icons-material/Category";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import DashboardIcon from "@mui/icons-material/Dashboard";
import EventNoteIcon from "@mui/icons-material/EventNote";
import GroupIcon from "@mui/icons-material/Group";
import HistoryIcon from "@mui/icons-material/History";
import Inventory2Icon from "@mui/icons-material/Inventory2";
import LogoutIcon from "@mui/icons-material/Logout";
import MenuIcon from "@mui/icons-material/Menu";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import { AppBar, Box, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, Toolbar, Typography } from "@mui/material";
import { useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../providers/AuthContext";
import { PERMISSIONS } from "../../shared/constants/permissions";
import type { Permission } from "../../shared/constants/permissions";

const DRAWER_WIDTH = 260;

interface MenuItem {
  label: string;
  path: string;
  permission: Permission;
  icon: React.ReactNode;
}

interface MenuGroup {
  title: string;
  items: MenuItem[];
}

/** Sol menü (dokuman 6): ogeler permission'a gore gizlenir. */
const MENU: MenuGroup[] = [
  {
    title: "Genel",
    items: [
      { label: "Dashboard", path: "/", permission: PERMISSIONS.reportRead, icon: <DashboardIcon /> },
    ],
  },
  {
    title: "Yonetim",
    items: [
      { label: "Isletmeler", path: "/businesses", permission: PERMISSIONS.businessRead, icon: <BusinessIcon /> },
      { label: "Kategoriler", path: "/categories", permission: PERMISSIONS.categoryRead, icon: <CategoryIcon /> },
      { label: "Hizmetler", path: "/services", permission: PERMISSIONS.serviceRead, icon: <AppRegistrationIcon /> },
      { label: "Kullanicilar", path: "/users", permission: PERMISSIONS.userRead, icon: <GroupIcon /> },
      { label: "Randevular", path: "/appointments", permission: PERMISSIONS.appointmentRead, icon: <EventNoteIcon /> },
    ],
  },
  {
    title: "Ticari",
    items: [
      { label: "Abonelikler", path: "/subscriptions", permission: PERMISSIONS.subscriptionRead, icon: <AttachMoneyIcon /> },
      { label: "Finans", path: "/finance", permission: PERMISSIONS.financeRead, icon: <BarChartIcon /> },
      { label: "Stok & Paketler", path: "/stock", permission: PERMISSIONS.stockRead, icon: <Inventory2Icon /> },
    ],
  },
  {
    title: "Operasyon",
    items: [
      { label: "Bildirimler", path: "/notifications", permission: PERMISSIONS.notificationRead, icon: <NotificationsActiveIcon /> },
      { label: "Pazarlama", path: "/marketing", permission: PERMISSIONS.marketingRead, icon: <CampaignIcon /> },
      { label: "Isler", path: "/operations", permission: PERMISSIONS.operationsRead, icon: <AssessmentIcon /> },
      { label: "Audit Log", path: "/audit", permission: PERMISSIONS.auditRead, icon: <HistoryIcon /> },
    ],
  },
];

export function AdminLayout() {
  const [open, setOpen] = useState(true);
  const { auth, logout, has_permission } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handle_logout = () => {
    logout();
    navigate("/login");
  };

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton color="inherit" edge="start" onClick={() => setOpen(!open)} aria-label="menu ac/kapat">
            {open ? <ChevronLeftIcon /> : <MenuIcon />}
          </IconButton>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Rezervise Backoffice
          </Typography>
          <AccessibilityNewIcon fontSize="small" sx={{ mr: 1, display: "none" }} />
          <Typography variant="body2" sx={{ mr: 2 }}>
            {auth?.email} ({auth?.role})
          </Typography>
          <IconButton color="inherit" onClick={handle_logout} aria-label="cikis yap">
            <LogoutIcon />
          </IconButton>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: open ? DRAWER_WIDTH : 0,
          flexShrink: 0,
          "& .MuiDrawer-paper": { width: DRAWER_WIDTH, transition: "width .2s", overflowX: "hidden" },
        }}
      >
        <Toolbar />
        <List>
          {MENU.map((group) => {
            const visible = group.items.filter((item) => has_permission(item.permission));
            if (visible.length === 0) return null;
            return (
              <Box key={group.title}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="overline" color="text.secondary" sx={{ px: 2, display: "block" }}>
                  {group.title}
                </Typography>
                {visible.map((item) => {
                  const active = location.pathname === item.path || (item.path !== "/" && location.pathname.startsWith(item.path));
                  return (
                    <ListItemButton
                      key={item.path}
                      selected={active}
                      onClick={() => navigate(item.path)}
                      aria-current={active ? "page" : undefined}
                    >
                      <ListItemIcon>{item.icon}</ListItemIcon>
                      <ListItemText primary={item.label} />
                    </ListItemButton>
                  );
                })}
              </Box>
            );
          })}
        </List>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3, ml: open ? `${DRAWER_WIDTH}px` : 0 }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
