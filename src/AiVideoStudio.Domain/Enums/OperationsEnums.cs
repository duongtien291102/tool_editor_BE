namespace AiVideoStudio.Domain.Enums;
public enum AuditActionType{Create,Read,Update,Delete,Execute,Login,Logout,Security}
public enum NotificationType{Information,Success,Warning,Error,System}
public enum QuotaType{ApiRequests,StorageBytes,WorkflowRuns,RenderMinutes,Exports}
public enum MaintenanceStatus{Pending,Running,Completed,Failed,Cancelled}
