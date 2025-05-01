package gr.jimmys.jimmysfoodzilla.dto;

public record WebOrderDTO(WebOrderItemDTO[] order, String userId){}