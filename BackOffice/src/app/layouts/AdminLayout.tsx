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
import RateReviewIcon from "@mui/icons-material/RateReview";
import StorefrontIcon from "@mui/icons-material/Storefront";
import { AppBar, Box, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, Toolbar, Typography, useMediaQuery, useTheme } from "@mui/material";
import { useEffect, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../providers/AuthContext";
import { OWNER_PERMISSIONS, PERMISSIONS } from "../../shared/constants/permissions";
import type { OwnerPermission, Permission } from "../../shared/constants/permissions";

const DRAWER_WIDTH = 260;

interface MenuItem {
  label: string;
  path: string;
  permission: Permission | OwnerPermission;
  icon: React.ReactNode;
}

interface MenuGroup {
  title: string;
  items: MenuItem[];
}

/** Admin menü (doküman 6): öğeler permission'a göre gizlenir. */
const ADMIN_MENU: MenuGroup[] = [
  {
    title: "Genel",
    items: [
      { label: "Dashboard", path: "/", permission: PERMISSIONS.reportRead, icon: <DashboardIcon /> },
    ],
  },
  {
    title: "Yönetim",
    items: [
      { label: "İşletmeler", path: "/businesses", permission: PERMISSIONS.businessRead, icon: <BusinessIcon /> },
      { label: "Kategoriler", path: "/categories", permission: PERMISSIONS.categoryRead, icon: <CategoryIcon /> },
      { label: "Hizmetler", path: "/services", permission: PERMISSIONS.serviceRead, icon: <AppRegistrationIcon /> },
      { label: "Kullanıcılar", path: "/users", permission: PERMISSIONS.userRead, icon: <GroupIcon /> },
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
      { label: "İşler", path: "/operations", permission: PERMISSIONS.operationsRead, icon: <AssessmentIcon /> },
      { label: "Audit Log", path: "/audit", permission: PERMISSIONS.auditRead, icon: <HistoryIcon /> },
    ],
  },
];

/** İşletme sahibi menü: yalnızca kendi işletmesi kapsamı. */
const OWNER_MENU: MenuGroup[] = [
  {
    title: "İşletmem",
    items: [
      { label: "Dashboard", path: "/owner", permission: OWNER_PERMISSIONS.dashboard, icon: <DashboardIcon /> },
      { label: "İşletme Bilgileri", path: "/owner/business", permission: OWNER_PERMISSIONS.business, icon: <StorefrontIcon /> },
      { label: "Hizmetler", path: "/owner/services", permission: OWNER_PERMISSIONS.services, icon: <AppRegistrationIcon /> },
      { label: "Çalışanlar", path: "/owner/employees", permission: OWNER_PERMISSIONS.employees, icon: <GroupIcon /> },
      { label: "Randevular", path: "/owner/appointments", permission: OWNER_PERMISSIONS.appointments, icon: <EventNoteIcon /> },
    ],
  },
  {
    title: "Finans & Stok",
    items: [
      { label: "Finans", path: "/owner/finance", permission: OWNER_PERMISSIONS.finance, icon: <BarChartIcon /> },
      { label: "Stok & Paketler", path: "/owner/stock", permission: OWNER_PERMISSIONS.stock, icon: <Inventory2Icon /> },
      { label: "Yorumlar", path: "/owner/reviews", permission: OWNER_PERMISSIONS.reviews, icon: <RateReviewIcon /> },
    ],
  },
];

export function AdminLayout() {
  const [open, setOpen] = useState(true);
  const { auth, logout, has_permission } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  // Mobil: çekmece geçici (temporary) — içerik üzerine biner.
  const is_mobile = useMediaQuery(theme.breakpoints.down("md"));

  // Mobilde yönlendirme sonrası menü kendiliğinden kapanır.
  useEffect(() => {
    if (is_mobile) setOpen(false);
  }, [is_mobile, location.pathname]);

  const is_owner = auth?.role === "BusinessOwner";
  const menu = is_owner ? OWNER_MENU : ADMIN_MENU;

  const handle_logout = () => {
    logout();
    navigate("/login");
  };

  const drawer = (
    <>
      <Toolbar />
      <List component="nav">
        {menu.map((group) => {
          const visible = group.items.filter((item) => has_permission(item.permission));
          if (visible.length === 0) return null;
          return (
            <Box key={group.title}>
              <Divider sx={{ my: 1 }} />
              <Typography variant="overline" color="text.secondary" sx={{ px: 2, display: "block" }}>
                {group.title}
              </Typography>
              {visible.map((item) => {
                const active =
                  location.pathname === item.path ||
                  (item.path !== "/" && item.path !== "/owner" && location.pathname.startsWith(item.path));
                return (
                  <ListItemButton
                    key={item.path}
                    selected={active}
                    onClick={() => {
                      navigate(item.path);
                      if (is_mobile) setOpen(false);
                    }}
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
    </>
  );

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton color="inherit" edge="start" onClick={() => setOpen(!open)} aria-label="menü aç/kapat">
            {open ? <ChevronLeftIcon /> : <MenuIcon />}
          </IconButton>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontSize: { xs: "1rem", sm: "1.25rem" }, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
            Rezervise {is_owner ? "İşletme Paneli" : "Backoffice"}
          </Typography>
          <AccessibilityNewIcon fontSize="small" sx={{ mr: 1, display: "none" }} />
          <Typography variant="body2" sx={{ mr: 2, display: { xs: "none", sm: "block" }, overflow: "hidden", textOverflow: "ellipsis", maxWidth: 260 }}>
            {auth?.email} ({auth?.role})
          </Typography>
          <IconButton color="inherit" onClick={handle_logout} aria-label="çıkış yap">
            <LogoutIcon />
          </IconButton>
        </Toolbar>
      </AppBar>

      {is_mobile ? (
        // Mobil: içerik üzerine binen geçici çekmece.
        <Drawer
          variant="temporary"
          open={open}
          onClose={() => setOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{ display: "block", "& .MuiDrawer-paper": { width: DRAWER_WIDTH } }}
        >
          {drawer}
        </Drawer>
      ) : (
        <Drawer
          variant="permanent"
          sx={{
            width: open ? DRAWER_WIDTH : 0,
            flexShrink: 0,
            "& .MuiDrawer-paper": {
              width: DRAWER_WIDTH,
              transition: "width .2s",
              overflowX: "hidden",
            },
          }}
        >
          {drawer}
        </Drawer>
      )}

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          // Mobilde çekmece kalici olmadigindan boşluk yok; masaüstünde açık çekmece kaydırır.
          p: { xs: 1.5, sm: 2, md: 3 },
          ml: { md: open ? `${DRAWER_WIDTH}px` : 0 },
          width: { xs: "100%", md: "auto" },
          overflowX: "hidden",
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
