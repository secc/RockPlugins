<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Interactions.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.Interactions" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog ID="mdInteractions" runat="server" Title="Visits & Visitors" OnSaveClick="mdInteractions_SaveClick" SaveButtonText="Close" SaveButtonCssClass="btn btn-link" CancelLinkVisible="false">
            <Content>
                <%-- Modal Content --%>
                <div class="panel-body" style="text-align:center;">
                    <div id="app">
                        <div class="row" style="padding-bottom:3em;">
                            <div class="col-xs 12 col-sm-6">
                                <div v-for="object in values">
                                    <h3>{{object.Name}}</h3>
                                    <p>Visits: {{object.Visits}} | Visitors: {{object.Visitors}}</p>
                                </div>
                            </div>
                            <div class="col-xs 12 col-sm-6">
                                <div class="chart-container">
                                    <canvas id="visitorChart"></canvas>
                                </div>
                            </div>
                        </div>
                     </div>
                    <div class="row">
                        <div class="chart-container">
                            <canvas id="myChart"></canvas>
                        </div>
                    </div>
                    
                    
                 </div>
            </Content>
        </Rock:ModalDialog>

        <div class="panel panel-block">
            <div class="panel-body text-center">
                <asp:LinkButton ID="btnInteractionsModal" runat="server"  OnClick="btnInteractionsModal_Click" CausesValidation="false" CssClass="btn btn-primary">
                    Interactions
                </asp:LinkButton>
            </div>
        </div>
        <script>
            function modalScript(thisPageId) {

                //app
                const app = {
                    data() {
                        return {
                            pageId: thisPageId,
                            values: [],
                            chartData: []
                        }
                    },
                    methods: {
                        update(data) {
                            this.values = data;
                            console.log(data);
                        }
                    }
                }
                //get start and enddates
                var today = new Date();
                today = today.toLocaleDateString('en-US');
                var yesterday = new Date();
                yesterday.setDate(yesterday.getDate() - 1);
                var lastWeek = new Date();
                lastWeek.setDate(lastWeek.getDate() - 8);
                var lastMonth = new Date();
                lastMonth.setDate(lastMonth.getDate() - 31);

                myapp = Vue.createApp(app).mount('#app')
                // Get data from the API
                $.getJSON("/api/cms/dailyinteraction/" + thisPageId + "?startDate=" + lastMonth.toLocaleDateString('en-US') + "&endDate=" + today,
                    function (response) {
                        // Put the response in an array
                        var newDataArray = [];                        
                        response.forEach(newArrayFunction);
                        function newArrayFunction(item, index) {
                            var date = new Date(item.Date);
                            newDataArray[index] = {
                                Date: date,
                                Visits: item.Visits,
                                Staff: item.StaffVisitors,
                                Member: item.MemberVisitors,
                                Attendee: item.AttendeeVisitors,
                                Prospect: item.ProspectVisitors,
                                Anonymous: item.AnonymousVisitors
                            };
                        };

                        // Get Chart Data

                        var chartLabels = [];
                        var visitsData = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            chartLabels.push(newDataArray[i].Date.toLocaleDateString());
                            visitsData.push(newDataArray[i].Visits);
                        }
                        var staffVisitors = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            staffVisitors.push(newDataArray[i].Staff);
                        }
                        var memberVisitors = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            memberVisitors.push(newDataArray[i].Member);
                        }
                        var attendeeVisitors = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            attendeeVisitors.push(newDataArray[i].Attendee);
                        }
                        var prospectVisitors = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            prospectVisitors.push(newDataArray[i].Prospect);
                        }
                        var anonymousVisitors = [];
                        for (var i = 0; i < newDataArray.length; i++) {
                            anonymousVisitors.push(newDataArray[i].Anonymous);
                        }

                        newDataArray = newDataArray.reverse();

                        // Calculate values to pass into Vue                        
                        var Values = []
                        Values[0] = {
                            Name: 'Yesterday',
                            Visits: newDataArray[0].Visits,
                            Visitors: ( newDataArray[0].Staff + newDataArray[0].Member + newDataArray[0].Attendee + newDataArray[0].Prospect + newDataArray[0].Anonymous )
                        };
                        Values[1] = {
                            Name: 'Last Week',
                            Visits: newDataArray.slice(0, 8).reduce((accumulator, currentValue) => accumulator + (currentValue.Visits * 1), 0),
                            Visitors: newDataArray.slice(0, 8).reduce((accumulator, currentValue) => accumulator + ((currentValue.Staff + currentValue.Member + currentValue.Attendee + currentValue.Prospect + currentValue.Anonymous)*1),0)
                        };
                        Values[2] = {
                            Name: 'Last Month',
                            Visits: newDataArray.reduce((accumulator, currentValue) => accumulator + (currentValue.Visits * 1), 0),
                            Visitors: newDataArray.reduce((accumulator, currentValue) => accumulator + ((currentValue.Staff + currentValue.Member + currentValue.Attendee + currentValue.Prospect + currentValue.Anonymous) * 1), 0)
                        };

                        myapp.update(Values);

                        // Visits & Visitors Line Chart

                        var ctx = document.getElementById("myChart").getContext('2d');
                        var myChart = new Chart(ctx, {
                            type: 'line',
                            data:
                            {
                                labels: chartLabels,
                                datasets: [{
                                    label: 'Daily Visits',
                                    data: visitsData,
                                    fill: true,
                                    borderColor: "rgb(255, 139, 0)",
                                    lineTension: 0.1
                                },
                                {
                                    label: 'Staff Visitors',
                                    data: staffVisitors,
                                    fill: true,
                                    borderColor: "rgb(192, 104, 50)",
                                    lineTension: 0.1
                                },
                                {
                                    label: 'Member Visitors',
                                    data: memberVisitors,
                                    fill: true,
                                    borderColor: "rgb(129, 70, 99)",
                                    lineTension: 0.1
                                },
                                {
                                    label: 'Attendee Visitors',
                                    data: attendeeVisitors,
                                    fill: true,
                                    borderColor: "rgb(65, 35, 149)",
                                    lineTension: 0.1
                                },
                                {
                                    label: 'Prospect Visitors',
                                    data: prospectVisitors,
                                    fill: true,
                                    borderColor: "rgb(2, 0, 198)",
                                    lineTension: 0.1
                                },
                                {
                                    label: 'Anonymous Visitors',
                                    data: anonymousVisitors,
                                    fill: true,
                                    borderColor: "rgb(0, 0, 0)",
                                    lineTension: 0.1
                                }]
                            },
                            options: {
                                responsive: true,
                                
                            }
                        });

                        // Visitors Doughnut Chart
                        var visitorData = {
                            datasets: [{
                                data: [10, 20, 30, 40, 50]
                            }],

                            // These labels appear in the legend and in the tooltips when hovering different arcs
                            labels: [
                                'Staff',
                                'Members',
                                'Attendees',
                                'Prospects',
                                'Anonymous'
                            ]
                        };
                        var visitorOptions = {};
                        var ctx = document.getElementById("visitorChart").getContext('2d');
                        var myChart = new Chart(ctx, {
                            type: 'doughnut',
                            data: visitorData,
                            options: visitorOptions
                        });

                        
                    }
                );
            }
        </script>
    </ContentTemplate>
</asp:UpdatePanel>