﻿// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: my.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
[global::ProtoBuf.ProtoContract()]
public partial class CmdPushPowerBank : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplPushPowerBank : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_pbid")]
    public ulong RlPbid { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_result")]
    public uint RlResult { get; set; }

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_code")]
    public uint RlCode { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_lock")]
    public uint RlLock { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_bottom")]
    public uint RlBottom { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_qoe")]
    public uint RlQoe { get; set; }

    [global::ProtoBuf.ProtoMember(8, Name = @"rl_vol")]
    public uint RlVol { get; set; }

    [global::ProtoBuf.ProtoMember(9, Name = @"rl_cur")]
    public uint RlCur { get; set; }

    [global::ProtoBuf.ProtoMember(10, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdPushPowerBankForce : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplPushPowerBankForce : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_pbid")]
    public ulong RlPbid { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_result")]
    public uint RlResult { get; set; }

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_code")]
    public uint RlCode { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_lock")]
    public uint RlLock { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_bottom")]
    public uint RlBottom { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_qoe")]
    public uint RlQoe { get; set; }

    [global::ProtoBuf.ProtoMember(8, Name = @"rl_vol")]
    public uint RlVol { get; set; }

    [global::ProtoBuf.ProtoMember(9, Name = @"rl_cur")]
    public uint RlCur { get; set; }

    [global::ProtoBuf.ProtoMember(10, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdQueryNetworkInfo : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQueryNetworkInfo : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_type")]
    public uint RlType { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_mode")]
    public uint RlMode { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_status")]
    public uint RlStatus { get; set; }

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_conn")]
    public uint RlConn { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_csq")]
    public uint RlCsq { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_rsrp")]
    public uint RlRsrp { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_sinr")]
    public uint RlSinr { get; set; }

    [global::ProtoBuf.ProtoMember(8, Name = @"rl_wifi")]
    public uint RlWifi { get; set; }

    [global::ProtoBuf.ProtoMember(9, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdQueryTheInventory : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQueryTheInventoryOne : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_idok")]
    public uint RlIdok { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_lock")]
    public uint RlLock { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_charge")]
    public uint RlCharge { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_pbid")]
    public ulong RlPbid { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_qoe")]
    public uint RlQoe { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQueryTheInventory : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_num")]
    public uint RlNum { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_bank1")]
    public global::System.Collections.Generic.List<RplQueryTheInventoryOne> RlBank1s { get; } = new global::System.Collections.Generic.List<RplQueryTheInventoryOne>();

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdQueryServer : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_type")]
    public uint RlType { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQueryServer : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_type")]
    public uint RlType { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_add")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlAdd { get; set; } = "";

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_port")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlPort { get; set; } = "";

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdQueryCabinetAPN : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_index")]
    public uint RlIndex { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQueryCabinetAPN : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_index")]
    public uint RlIndex { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_valid")]
    public uint RlValid { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_mccmnc")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlMccmnc { get; set; } = "";

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_apn")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlApn { get; set; } = "";

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_un")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlUn { get; set; } = "";

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_pw")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlPw { get; set; } = "";

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdQuerySIMCardICCID : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplQuerySIMCardICCID : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_iccid")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlIccid { get; set; } = "";

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_imei")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlImei { get; set; } = "";

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class CmdResetCabinet : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RplResetCabinet : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_result")]
    public uint RlResult { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RptReturnThePowerBank : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_lock")]
    public uint RlLock { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_bottom")]
    public uint RlBottom { get; set; }

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_pbid")]
    public ulong RlPbid { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_qoe")]
    public uint RlQoe { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_vol")]
    public uint RlVol { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_cur")]
    public uint RlCur { get; set; }

    [global::ProtoBuf.ProtoMember(8, Name = @"rl_tmp")]
    public uint RlTmp { get; set; }

    [global::ProtoBuf.ProtoMember(9, Name = @"rl_limited")]
    public uint RlLimited { get; set; }

    [global::ProtoBuf.ProtoMember(10, Name = @"rl_code")]
    public uint RlCode { get; set; }

    [global::ProtoBuf.ProtoMember(11, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class SrvReturnThePowerBank : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_slot")]
    public uint RlSlot { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_result")]
    public uint RlResult { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

[global::ProtoBuf.ProtoContract()]
public partial class RptReportCabinetLogin : global::ProtoBuf.IExtensible
{
    private global::ProtoBuf.IExtension __pbn__extensionData;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    [global::ProtoBuf.ProtoMember(1, Name = @"rl_count")]
    public uint RlCount { get; set; }

    [global::ProtoBuf.ProtoMember(2, Name = @"rl_netmode")]
    public uint RlNetmode { get; set; }

    [global::ProtoBuf.ProtoMember(3, Name = @"rl_conn")]
    public uint RlConn { get; set; }

    [global::ProtoBuf.ProtoMember(4, Name = @"rl_csq")]
    public uint RlCsq { get; set; }

    [global::ProtoBuf.ProtoMember(5, Name = @"rl_rsrp")]
    public uint RlRsrp { get; set; }

    [global::ProtoBuf.ProtoMember(6, Name = @"rl_sinr")]
    public uint RlSinr { get; set; }

    [global::ProtoBuf.ProtoMember(7, Name = @"rl_wifi")]
    public uint RlWifi { get; set; }

    [global::ProtoBuf.ProtoMember(8, Name = @"rl_commsoftver")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlCommsoftver { get; set; } = "";

    [global::ProtoBuf.ProtoMember(9, Name = @"rl_commhardver")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlCommhardver { get; set; } = "";

    [global::ProtoBuf.ProtoMember(10, Name = @"rl_iccid")]
    [global::System.ComponentModel.DefaultValue("")]
    public string RlIccid { get; set; } = "";

    [global::ProtoBuf.ProtoMember(11, Name = @"rl_seq")]
    public uint RlSeq { get; set; }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion