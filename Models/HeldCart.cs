using System;
using System.Collections.ObjectModel;
using SupermarketPOS.ViewModels;

namespace SupermarketPOS.Models;

public class HeldCart
{
    public string Id { get; } = Guid.NewGuid().ToString().Substring(0, 8);
    public DateTime HeldAt { get; } = DateTime.Now;
    public ObservableCollection<CartItem> Items { get; set; } = new();
    
    public string Summary => $"{Items.Count} ta mahsulot - {HeldAt:HH:mm}";
}
