using MediatR;

namespace Nexus.CatalogService.Application.UseCases.DeleteProduct;

/// <summary>
/// Command xóa sản phẩm - thực ra là soft delete (MarkDeleted), không xóa thật.
/// Lý do giữ lại record: các order lịch sử vẫn cần reference tới product này.
///
/// RequestedByUserId dùng để check quyền: chỉ seller của product hoặc admin mới được xóa.
/// (logic check quyền hiện tại chưa implement trong handler - TODO)
/// </summary>
public record DeleteProductCommand(Guid ProductId, Guid RequestedByUserId) : IRequest;
// IRequest không có generic type → handler trả void (Unit trong MediatR)
