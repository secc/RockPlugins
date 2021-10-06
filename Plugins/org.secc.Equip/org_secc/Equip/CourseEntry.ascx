<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseEntry.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseEntry" %>
<style>
    .answers {
        font-size: 1.5em;
    }

        .answers .answer {
            border: solid 2px black;
            border-radius: 6px;
            margin: 5px 0px;
            box-shadow: 2px 2px 2px #0000005e;
            padding: 15px;
            cursor: pointer;
        }

            .answers .answer:hover {
                box-shadow: 3px 3px 2px #0000005e;
            }

        .answers .selected {
            border: solid 2px #428bca !important;
            box-shadow: 3px 3px 2px #0000005e;
        }

        .answers .incorrect {
            border: solid 2px #d95353 !important;
            box-shadow: 3px 3px 2px #0000005e;
        }

        .answers .correct {
            border: solid 2px #5cb85c !important;
            box-shadow: 3px 3px 2px #0000005e;
        }

        .answers .answer i {
            margin: 10px;
            font-size: 1.5em;
        }

        .answers .answer .single:before {
            content: '\f111'
        }

        .answers .selected .single:before {
            content: '\f058';
        }

        .answers .answer .multi:before {
            content: '\f0c8'
        }

        .answers .selected .multi:before {
            content: '\f14a';
        }

    .submit {
        margin: auto;
    }

    .btn-next {
        width: 50%;
    }

    @media (max-width: 760px) {
        .btn-next {
            width: 100%;
        }
    }
</style>


<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlTOC" CssClass="col-sm-2 table-of-contents">
            <div class="dropdown hidden visible-xs">
                <button class="btn btn-default dropdown-toggle btn-block" type="button" data-toggle="dropdown">
                    <asp:PlaceHolder runat="server" ID="phMobileTitle" />  
                    <span class="caret"></span>
                </button>
                <ul class="dropdown-menu">
                    <asp:PlaceHolder runat="server" ID="phMobileTOC" />
                </ul>
            </div>
            <div class="hidden-xs">
                <Rock:DynamicPlaceholder runat="server" ID="phTOC" />
            </div>
            <br />
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlElements" CssClass="col-sm-10 lesson-content">
            <Rock:DynamicPlaceholder runat="server" ID="phContent" />
            <br />
            <br />
            <Rock:BootstrapButton runat="server" ID="btnNext" Text="Continue" OnClick="btnNext_Click" CssClass="btn btn-primary btn-lg center-block btn-next" />
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlFail" CssClass="col-sm-10 text-center lesson-fail">
            <h2>Chapter Failed
            </h2>
            We are sorry, but you did not meet the requirements for completing this chapter. You can restart this chapter.
            <br />
            <br />
            <Rock:BootstrapButton runat="server" ID="btnFail" Text="Continue" OnClick="btnFail_Click" CssClass="btn btn-primary btn-lg center-block btn-next" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlSuccess" CssClass="text-center lesson-complete">
            <h2>Course Complete
            </h2>
            Congratulations. You have succesfuly completed this course.
            <br />
            <br />
            <Rock:BootstrapButton runat="server" ID="btnComplete" Text="Done" OnClick="btnComplete_Click" CssClass="btn btn-primary btn-lg center-block btn-next" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlRedirectDebug" Visible="false">
            <Rock:NotificationBox runat="server" ID="nbRedirectDebug" NotificationBoxType="Info" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
