﻿using System;
using System.Collections.Generic;

namespace urbanBackend.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public string? UserId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public virtual Product? Product { get; set; }
}
