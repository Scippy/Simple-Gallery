<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="AddPhoto.ascx.vb" Inherits="Ventrian.SimpleGallery.AddPhoto" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="SimpleGallery" TagName="GalleryMenu" Src="Controls\GalleryMenu.ascx" %>
<%@ Register TagPrefix="SimpleGallery" TagName="EditPhotos" Src="Controls\EditPhotos.ascx" %>
<SimpleGallery:GalleryMenu ID="ucGalleryMenu" runat="server" ShowCommandBar="False" ShowSeparator="True" />



<%--<asp:PlaceHolder ID="imageToolsScripts" runat="server" EnableViewState="False" />--%>
<asp:PlaceHolder ID="exifScripts" runat="server" EnableViewState="False" />

<style>
    .flex-container {
        display: flex;
        flex-wrap: wrap;
    }
    .img-container {
      display: inline;
      position: relative;
      margin: 5px;
    }
    .close {
      position: absolute;
      right: 0;
    }

    .rotate 
     {
      position: absolute;
      left: 0;
    }

    .iborder {
      padding:5px;
      border:8px solid #999999;
      background-color: #e6e6e6;
      }
  
</style>

<script runat="server">

    Sub AttachCustomHeader(ByVal CustomHeader As String)
        Dim HtmlHead As HtmlHead = Page.FindControl("Head")
        If Not (HtmlHead Is Nothing) Then
            HtmlHead.Controls.Add(New LiteralControl(CustomHeader))
        End If
    End Sub
</script>

<script type="text/javascript"> 
    var ua = window.navigator.userAgent; var msie = ua.indexOf("MSIE "); if (msie > 0 || !!navigator.userAgent.match(/Trident.*rv\:11\./))
        document.write('<script src="//cdn.jsdelivr.net/npm/es6-promise@4/dist/es6-promise.auto.min.js"><\/scr' + 'ipt>'); 
</script> 
<%--<script src="//cdnjs.cloudflare.com/ajax/libs/bluebird/3.3.5/bluebird.min.js"></script>--%>
<%--<script src="//cdn.jsdelivr.net/npm/promise-polyfill@8/dist/polyfill.min.js"></script>--%>
<script type="text/javascript" src='<%= ResolveUrl("JS/compress.js") %>'></script>
<script type="text/javascript" src="//cdn.jsdelivr.net/npm/gasparesganga-jquery-loading-overlay@2.1.6/dist/loadingoverlay.min.js"></script>

<script language="javascript" type="text/javascript">
if (!Array.from) {
  Array.from = (function () {
    var toStr = Object.prototype.toString;
    var isCallable = function (fn) {
      return typeof fn === 'function' || toStr.call(fn) === '[object Function]';
    };
    var toInteger = function (value) {
      var number = Number(value);
      if (isNaN(number)) { return 0; }
      if (number === 0 || !isFinite(number)) { return number; }
      return (number > 0 ? 1 : -1) * Math.floor(Math.abs(number));
    };
    var maxSafeInteger = Math.pow(2, 53) - 1;
    var toLength = function (value) {
      var len = toInteger(value);
      return Math.min(Math.max(len, 0), maxSafeInteger);
    };

    // The length property of the from method is 1.
    return function from(arrayLike/*, mapFn, thisArg */) {
      // 1. Let C be the this value.
      var C = this;

      // 2. Let items be ToObject(arrayLike).
      var items = Object(arrayLike);

      // 3. ReturnIfAbrupt(items).
      if (arrayLike == null) {
        throw new TypeError('Array.from requires an array-like object - not null or undefined');
      }

      // 4. If mapfn is undefined, then let mapping be false.
      var mapFn = arguments.length > 1 ? arguments[1] : void undefined;
      var T;
      if (typeof mapFn !== 'undefined') {
        // 5. else
        // 5. a If IsCallable(mapfn) is false, throw a TypeError exception.
        if (!isCallable(mapFn)) {
          throw new TypeError('Array.from: when provided, the second argument must be a function');
        }

        // 5. b. If thisArg was supplied, let T be thisArg; else let T be undefined.
        if (arguments.length > 2) {
          T = arguments[2];
        }
      }

      // 10. Let lenValue be Get(items, "length").
      // 11. Let len be ToLength(lenValue).
      var len = toLength(items.length);

      // 13. If IsConstructor(C) is true, then
      // 13. a. Let A be the result of calling the [[Construct]] internal method 
      // of C with an argument list containing the single item len.
      // 14. a. Else, Let A be ArrayCreate(len).
      var A = isCallable(C) ? Object(new C(len)) : new Array(len);

      // 16. Let k be 0.
      var k = 0;
      // 17. Repeat, while k < len… (also steps a - h)
      var kValue;
      while (k < len) {
        kValue = items[k];
        if (mapFn) {
          A[k] = typeof T === 'undefined' ? mapFn(kValue, k) : mapFn.call(T, kValue, k);
        } else {
          A[k] = kValue;
        }
        k += 1;
      }
      // 18. Let putStatus be Put(A, "length", len, true).
      A.length = len;
      // 20. Return A.
      return A;
    };
  }());
}
</script>

<script language="javascript" type="text/javascript">
    const compress = new Compress()

    function addElement(parentId, elementTag, elementId, html) {
        // Adds an element to the document
        var p = document.getElementById(parentId);
        var newElement = document.createElement(elementTag);
        newElement.setAttribute('id', elementId);
        newElement.innerHTML = html;
        p.appendChild(newElement);
    }

    var selected = [];

    function loadpreview() {
        $.LoadingOverlay("show");
        //$('#pdiv').html('');
        //selected = [];

        var files = $('#<%=fupFile.ClientID %>')[0].files;
        var fileListAsArray = Array.from(files)
        for (var i = fileListAsArray.length - 1; i >= 0; i--)
        {
            if (fileListAsArray[i].type !== "image/jpg"
                && fileListAsArray[i].type !== "image/jpeg"
            && fileListAsArray[i].type !== "image/png"
            && fileListAsArray[i].type !== "image/gif")
            {
                fileListAsArray.splice(i,1);
            }
        }

        function create_UUID(){
            var dt = new Date().getTime();
            var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                var r = (dt + Math.random()*16)%16 | 0;
                dt = Math.floor(dt/16);
                return (c=='x' ? r :(r&0x3|0x8)).toString(16);
            });
            return uuid;
        }

        compress.compress(fileListAsArray, {
            size: 2, // the max size in MB, defaults to 2MB
            quality: .75, // the quality of the image, max is 1,
            maxWidth: 1024, // the max width of the output image, defaults to 1920px
            maxHeight: 768, // the max height of the output image, defaults to 1920px
            resize: true, // defaults to true, set false if you do not want to resize the image width and height
        }).then(function (conversions)
        {

            var fu = document.getElementById('<%=fupFile.ClientID %>');
            if (fu != null) {
                document.getElementById('<%=fupFile.ClientID %>').outerHTML = fu.outerHTML;
            }

            for (var i = 0; i < conversions.length; i++) {
                var uid = create_UUID();
                const output = { 'id': uid, 'photo': conversions[i] };
                selected.push(output);
                
                addElement('pdiv', 'span', 'simage' + i, '<div class="img-container"><img class="iborder" style="max-width: 120px;max-height: 120px;" alt="" src="' + output.photo.prefix + output.photo.data + '"><img alt="remove" data-sindex="' + output.id + '" class="close" src="<%= ResolveUrl("images/closeIcon.png")%>" /></div>');
            }
          <%--  for (var i = 0; i < selected.length; i++) {

                const output = selected[i];
                //addElement('pdiv', 'span', 'simage' + i, '<div class="img-container"><img alt="rotate" data-sindex="'+output.id+'" class="rotate" src="<%= ResolveUrl("images/rotate.png")%>" /><img class="iborder" style="max-width: 120px;max-height: 120px;" alt="" src="' + output.photo.prefix + output.photo.data + '"><img alt="remove" data-sindex="'+output.id+'" class="close" src="<%= ResolveUrl("images/closeIcon.png")%>" /></div>');
                addElement('pdiv', 'span', 'simage' + i, '<div class="img-container"><img class="iborder" style="max-width: 120px;max-height: 120px;" alt="" src="' + output.photo.prefix + output.photo.data + '"><img alt="remove" data-sindex="' + output.id + '" class="close" src="<%= ResolveUrl("images/closeIcon.png")%>" /></div>');

            }--%>
            HideSpinner();

            $('img.close').click(function () {
                var myindex = $(this).attr('data-sindex');
                var myimagdiv = $(this).closest('div');
                var myimagdivistarget = $(myimagdiv).hasClass('img-container');
                if (myindex !== undefined && myimagdivistarget === true) {
                    $(myimagdiv).html('');
                    remove(selected, myindex);
                }
            });

            //$('img.rotate').click(function(){
            //    var myindex = $(this).attr('data-sindex');
            //    var myimagdiv = $(this).closest('div');
            //    var myimagdivistarget = $(myimagdiv).hasClass('img-container');
            //    if (myindex !== undefined && myimagdivistarget === true)
            //    {

            //       // $(myimagdiv).html('');
            //        //remove(selected, myindex);
            //        var myphoto;
            //        for (var i = selected.length; i--;)
            //        {
            //            if (selected[i].id === myindex)
            //            {
            //                myphoto = selected[i];
            //            }
            //        }

            //         //$(this).closest('div').find('img.iborder').rotate({angle:90});


            //       // var newimg = rotateBase64Image(myphoto.photo.prefix + myphoto.photo.data);

            //        //myphoto.photo.data = newimg.substring(myphoto.photo.prefix.length);

            //       // $(this).closest('div').find('img.iborder').attr('src',newimg);

            //        rotateBase64Image(myphoto.photo.prefix + myphoto.photo.data,
            //            function callback(base64data) {
            //                $(this).closest('div').find('img.iborder').attr('src',base64data);
            //            });
            //    }
            //});
        }, function (err) {
            console.log(err); // Error: "It broke"
            HideSpinner();
            alert('Error occured. Try selecting fewer number of images. ' + (err.description !== undefined ? 'Detail: ' + err.description : '' ));

        });
    }



   

    function remove(arr, item)
    {
        for (var i = arr.length; i--;)
        {
            if (arr[i].id === item)
            {
                arr.splice(i, 1);
            }
        }
    }

    function ShowSpinner()
    {
        $.LoadingOverlay("show", {image: "",progress: true});
    }

    function HideSpinner()
    {
        $.LoadingOverlay('hide', true);
    }

    function StartUpload()
    {
        if (selected.length > 0)
        {
            ShowSpinner();return UploadFiles(0, Math.round(100 / selected.length));
        }
        else
        {
            alert('Plese select files to upload!');
        }
        return false;
    }

    
    function UploadFiles(lastpercent, percentcunk)
    {
        var data = new FormData();
        var newpercent = lastpercent + percentcunk;
		if (newpercent > 100)
		{
			newpercent = 100;
		}

        if (selected.length > 0)
        {
            $.LoadingOverlay("progress", newpercent);

            var myphoto1 = selected.pop();
            var myphoto = myphoto1.photo;
            data.append("FileName", myphoto.alt);
            data.append("ContentType", myphoto.ext);
            data.append("Fdata", myphoto.data);
            data.append("AlbumID", '<%= Request("AlbumID") %>');
            data.append("TabID", '<%= Request("TabID") %>');
            data.append("ModuleID", '<%=Me.ModuleID%>');
            data.append("BatchID", '<%=Me.litBatchID.Value%>');

            $.ajax({
                type: "POST",
                url: "<%= GetUploadUrl() %>",
                contentType: false,
                processData: false,
                data: data,
                success: function (serverData) {
                    console.log(serverData);

                    if (serverData == "" || serverData == "-1" || serverData == "-2" || serverData == "-3") {
                        switch (serverData) {
                            case "":
                                alert("An error has occurred. Please see the administrator event log.");
                                break;

                            case "-1":
                                alert("An error has occurred. File limit exceeded on portal.");
                                break;

                            case "-2":
                                alert("An error has occurred. Unable to authenticate.");
                                break;

                            case "-3":
                                alert("An error has occurred. Please see the administrator event log.");
                                break;
                        }
                    }

                    if (selected.length > 0) 
                    {
                        UploadFiles(newpercent, percentcunk);
                    }
                    else
                    {
                        //HideSpinner();
                        console.log('Upload completed.');
                        document.getElementById("<%=cmdNext2.ClientID %>").click();
                    }
                }
            });
        }
        return false;
    }
</script>


<div align="left">
    <table cellspacing="0" cellpadding="0" width="600" summary="Wizard Design Table">
        <tr>
            <td width="50" height="50" align="center" valign="middle">
                <asp:Image ID="imgStep" runat="server" ImageUrl="~/DesktopModules/SimpleGallery/Images/iconStep1.gif" Width="48" Height="48" />
            </td>
            <td>
                <asp:Label ID="lblStep" runat="server" CssClass="NormalBold" /><br />
                <asp:Label ID="lblStepDescription" runat="server" CssClass="Normal" /><br />
                <asp:Label ID="lblRequiresApproval" runat="server" CssClass="NormalRed" ResourceKey="RequiresApproval" EnableViewState="False" Visible="False" />
            </td>
        </tr>
    </table>
</div>
<hr size="1" />

<asp:Panel ID="pnlStep1" runat="server">
    <asp:PlaceHolder ID="phStep1a" runat="server">
        <table cellspacing="0" cellpadding="2" width="600" summary="Select Album Design Table" align="center">
            <tr>
                <td colspan="2">
                    <asp:RadioButton ID="rdoSelectExisting" runat="server" Text="Selecting Existing Album" ResourceKey="SelectExisting" CssClass="NormalBold" GroupName="Step1" /></td>
            </tr>
            <tr valign="top">
                <td class="SubHead" width="150">
                    <dnn:Label ID="plAlbum" runat="server" ControlName="drpAlbums" Suffix=":"></dnn:Label>
                </td>
                <td>
                    <asp:DropDownList ID="drpAlbums" runat="server" DataTextField="CaptionIndented" DataValueField="AlbumID" Width="300px" />
                    <asp:CustomValidator ID="valSelectExisting" runat="server" CssClass="NormalRed" ResourceKey="valSelectExisting" Display="Dynamic"
                        ControlToValidate="drpAlbums" ErrorMessage="<br>You must select an existing album."></asp:CustomValidator>
                </td>
            </tr>
        </table>
        <hr size="1" />
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="phStep1b" runat="server">
        <table cellspacing="0" cellpadding="2" width="600" summary="Select Album Design Table" align="center">
            <tr>
                <td colspan="2">
                    <asp:RadioButton ID="rdoCreateNew" runat="server" Text="Create New Album" ResourceKey="CreateNew" CssClass="NormalBold" GroupName="Step1" /></td>
            </tr>
            <tr valign="top" runat="server" id="trParentAlbum">
                <td class="SubHead" width="150">
                    <dnn:Label ID="plParentAlbum" runat="server" ResourceKey="ParentAlbum" Suffix=":" ControlName="drpParentAlbum"></dnn:Label>
                </td>
                <td>
                    <asp:DropDownList ID="drpParentAlbum" DataValueField="AlbumID" DataTextField="CaptionIndented" runat="server" Width="300px"></asp:DropDownList></td>
            </tr>
            <tr valign="top">
                <td class="SubHead" nowrap="nowrap" width="150">
                    <dnn:Label ID="plCaption" runat="server" ResourceKey="Caption" Suffix=":" ControlName="txtCaption"></dnn:Label>
                </td>
                <td align="left" width="450">
                    <asp:TextBox ID="txtCaption" CssClass="NormalTextBox" runat="server" Width="300" MaxLength="255"></asp:TextBox>
                    <asp:CustomValidator ID="valSelectNew" runat="server" CssClass="NormalRed" ResourceKey="valSelectNew" Display="Dynamic"
                        ErrorMessage="<br>You Must Enter a Valid Caption" ControlToValidate="txtCaption" ValidateEmptyText="true" />
                </td>
            </tr>
            <tr valign="top">
                <td class="SubHead" width="150">
                    <dnn:Label ID="plDescription" runat="server" Suffix=":" ControlName="txtDescription"></dnn:Label>
                </td>
                <td>
                    <asp:TextBox ID="txtDescription" CssClass="NormalTextBox" runat="server" Width="300" Columns="30"
                        MaxLength="255" TextMode="MultiLine" Rows="5"></asp:TextBox></td>
            </tr>
        </table>
    </asp:PlaceHolder>
</asp:Panel>
<asp:Panel ID="pnlStep2" runat="server">
    <asp:HiddenField ID="litBatchID" runat="server" />
    <div>
        <asp:FileUpload ID="fupFile" runat="server" AllowMultiple="true" accept=".jpg, .jpeg, .png, .gif" ClientIDMode="Static" onchange="loadpreview()" />
        <asp:RegularExpressionValidator ID="rexp" runat="server" ControlToValidate="fupFile"
            ErrorMessage="Only .gif, .jpg, .png and .jpeg"
            ValidationExpression="(.*\.([Gg][Ii][Ff])|.*\.([Jj][Pp][Gg])|.*\.([pP][nN][gG])$)">

        </asp:RegularExpressionValidator>
        <asp:Button runat="server" ID="btnUploadFiles"
            UseSubmitBehaviour="false" OnClick="btnUploadFiles_OnClick" resourcekey="btnUploadFiles" Visible="False" />

        <asp:Button runat="server" ID="Button1" OnClientClick="ShowSpinner();return UploadFiles(0);" UseSubmitBehaviour="false" resourcekey="btnUploadFiles" Visible="False" />

        <div style="margin-top: 20px;" id="pdiv" />
      
    </div>
    <br />
    <br />
    <div class="flex-container">
        <asp:Repeater runat="server" ID="addedPhotosRepeater" EnableViewState="True" OnItemDataBound="addedPhotosRepeater_OnItemDataBound" OnItemCommand="addedPhotosRepeater_ItemCommand">
            <ItemTemplate>
                <table class="photo-frame">
                    <tbody>
                        <tr>
                            <td class="topx--"></td>
                            <td class="top-x-"></td>
                            <td class="top--x"></td>
                        </tr>
                        <tr>
                            <td class="midx--"></td>
                            <td valign="top">
                                <asp:Image runat="server" ID="addedPhoto" CssClass="photo_198" />
                                <span style="align-content: center">
                                    <asp:LinkButton ID="cmdrotate" runat="server" CssClass="CommandButton" Text="Rotate" BorderStyle="none" CausesValidation="false"></asp:LinkButton>
                                </span>
                            </td>
                            <td class="mid--x"></td>
                        </tr>
                        <tr>
                            <td class="botx--"></td>
                            <td class="bot-x-"></td>
                            <td class="bot--x"></td>
                        </tr>
                    </tbody>
                </table>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</asp:Panel>

<div style="width: 100%;">
    <SimpleGallery:EditPhotos ID="ucEditPhotos" runat="server" />
</div>

<asp:Panel ID="pnlWizard" runat="server">
    <div align="center">
        <br />

        

        <asp:ImageButton ID="imgPrevious" runat="server" ImageUrl="~\images\lt.gif" ImageAlign="AbsBottom" />
        <asp:LinkButton ID="cmdPrevious" resourcekey="PreviousStep" runat="server" CssClass="CommandButton" Text="Previous"
            BorderStyle="none" />
        <asp:ImageButton ID="imgCancel" runat="server" ImageUrl="~\DesktopModules\SimpleGallery\images\iconCancel.gif" ImageAlign="AbsBottom" CausesValidation="False" Style="padding-left: 20px;" />
        <asp:LinkButton ID="cmdCancel" runat="server" CssClass="CommandButton" ResourceKey="cmdCancel" Text="Cancel" BorderStyle="none" CausesValidation="False" Style="padding-right: 20px;" />
        
        <% If pnlStep2.Visible = True Then%>
             <asp:LinkButton ID="LinkButton1" OnClientClick="StartUpload(); return false;" UseSubmitBehaviour="false" resourcekey="NextStep" runat="server" CssClass="CommandButton" Text="Next" BorderStyle="none" />
             <asp:ImageButton ID="ImageButton1" OnClientClick="StartUpload(); return false;" UseSubmitBehaviour="false" runat="server" ImageUrl="~\images\rt.gif" ImageAlign="AbsBottom" />
            <span style="display:none"><asp:Button ID="cmdNext2" runat="server" Text="Next2" /></span>
           <% Else %>  
            <asp:LinkButton ID="cmdNext" resourcekey="NextStep" runat="server" CssClass="CommandButton" Text="Next" BorderStyle="none" />
            <asp:ImageButton ID="imgNext" runat="server" ImageUrl="~\images\rt.gif" ImageAlign="AbsBottom" />
          <% End If %>
    </div>
</asp:Panel>

<asp:Panel ID="pnlSave" runat="server">
    <p align="center">
        <br />
        <asp:ImageButton ID="imgSave" runat="server" ImageUrl="~\DesktopModules\SimpleGallery\images\iconSave.gif" ImageAlign="AbsBottom" />
        <asp:LinkButton ID="cmdSave" runat="server" CssClass="CommandButton" ResourceKey="cmdSave" Text="Save this batch" BorderStyle="none" />
    </p>
</asp:Panel>


<ul id="thumbnails" class="sg_photolist"></ul>
