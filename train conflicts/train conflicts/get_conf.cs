using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace train_conflicts
{
    class get_conf
    {
        public DataTable read_file(string path)
        {
            StreamReader sr = null;
            try
            {
                sr = new System.IO.StreamReader(path);
            }
            //System.Collections.ListDictionaryInternal

            catch (Exception)
            {
                return null;
            }

            string headers = sr.ReadLine();
            string[] header_list = headers.Split(',');
            DataTable file = new DataTable();
            foreach (var h in header_list)
            {
                DataColumn dc = new DataColumn(h, typeof(string));
                file.Columns.Add(dc);
            }
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                DataRow dr = file.NewRow();
                string[] line_list = line.Split(',');
                for (int i = 0; i < line_list.Length; i++)
                {
                    dr[i] = line_list[i];
                }
                file.Rows.Add(dr);
            }
            return file;
        }
        public void main(DataTable train_path, DataTable zone)
        {
            init(train_path);
            FolderSelectDialog fs = new FolderSelectDialog();
            fs.Title = "请选择输出文件存放路径";
            string strpath = null;
            bool is_select = fs.ShowDialog();
            while (is_select == false)
            {
                MessageBox.Show("请选择文件夹!!!");
                is_select = fs.ShowDialog();
            }
            strpath = fs.FileName;
            DataTable node = define_node(), road_link = define_road_link(), agent = define_agent(), agent_type = define_agent_type();
            DataTable bt_node = define_node(), bt_road_link = define_road_link(), bt_agent = define_agent();
            DataTable conf_node = define_node(), conf_road_link = define_road_link(), conf_agent = define_agent();
            for (int station = 1; station <= station_num; station++)
            {
                for (int t = 0; t < time_len; t++)
                {
                    int now_time = int2HHMM(t);
                    DataRow now_node = node.NewRow();
                    int zone_id = get_zone_id(zone, station, now_time);
                    int node_type_id = get_node_type(train_path, station, now_time);
                    g_train_node(now_node, station, now_time, zone_id, node_type_id);
                    node.Rows.Add(now_node);
                }
            }//space time node
            List<conf> all_confs = get_all_conf(train_path);
            int conf_road_link_id = 1, conf_agent_id = 1;
            for (int i = 0; i < all_confs.Count; i++)
            {
                DataRow conf_node1 = conf_node.NewRow();
                DataRow conf_node2 = conf_node.NewRow();
                g_conf_node(conf_node1, all_confs[i].station, all_confs[i].start_time, all_confs[i].flag);
                conf_node.Rows.Add(conf_node1);
                if (all_confs[i].start_time != all_confs[i].end_time)
                {
                    g_conf_node(conf_node2, all_confs[i].station, all_confs[i].end_time, all_confs[i].flag);
                    conf_node.Rows.Add(conf_node2);
                }  
                DataRow conf_link = conf_road_link.NewRow();
                g_conf_road_link(conf_link, all_confs[i].station, all_confs[i].start_time, all_confs[i].end_time, all_confs[i].flag, conf_road_link_id++);
                conf_road_link.Rows.Add(conf_link);
                DataRow conf_agent_ = conf_agent.NewRow();
                g_conf_agent(conf_agent_, all_confs[i].station, all_confs[i].start_time, all_confs[i].end_time, all_confs[i].flag, conf_agent_id++);
                conf_agent.Rows.Add(conf_agent_);
            }//train conf
            int road_link_id = 1, agent_id = 1;
            for (int i = 0; i < train_path.Rows.Count; i++)
            {
                List<int[]> node_seq = trans_node_seq((string)train_path.Rows[i][1]);
                List<int> time_seq = trans_time_seq((string)train_path.Rows[i][2]);
                int flag = int.Parse((string)train_path.Rows[i][9]);
                DataRow agent_row = agent.NewRow();
                agent_row[0] = agent_id++;
                string time_period = null, node_sequence = null, time_sequence = null;
                int cost = 0;
                for (int ii = 0; ii < node_seq.Count - 1; ii++)
                {
                    DataRow road_link_ = road_link.NewRow();
                    int f_s = node_seq[ii][0];
                    int t_s = node_seq[ii + 1][0];
                    int f_t = time_seq[ii];
                    int t_t = time_seq[ii + 1];
                    g_train_link(road_link_, f_s, t_s, f_t, t_t, road_link_id++);
                    road_link.Rows.Add(road_link_);
                    if (ii == 0)
                    {
                        int o_zone_id = get_zone_id(zone, f_s, f_t);
                        agent_row[1] = o_zone_id;
                        agent_row[3] = f_s * 100000 + f_t;
                        time_period += time_int2string(f_t) + "_";
                        node_sequence += (f_s * 100000 + f_t).ToString() + ";" + (t_s * 100000 + t_t).ToString();
                        time_sequence += time_int2string(f_t) + ";" + time_int2string(t_t);
                        cost = f_t;
                    }
                    else if (ii == node_seq.Count - 2)
                    {
                        int d_zone_id = get_zone_id(zone, t_s, t_t);
                        agent_row[2] = d_zone_id;
                        agent_row[4] = t_s * 100000 + t_t;
                        time_period += time_int2string(t_t);
                        node_sequence += ";" + (t_s * 100000 + t_t).ToString();
                        time_sequence += ";" + time_int2string(t_t);
                        cost = time_sub(t_t, cost);
                    }
                    else
                    {
                        node_sequence += ";" + (t_s * 100000 + t_t).ToString();
                        time_sequence += ";" + time_int2string(t_t);
                    }
                }
                agent_row[5] = "train_path";
                agent_row[6] = time_period;
                agent_row[7] = 1;
                for (int ii = 8; ii <= 10; ii++)
                {
                    agent_row[ii] = cost;
                }
                agent_row[11] = node_sequence;
                agent_row[12] = time_sequence;
                agent.Rows.Add(agent_row);
            }//train path
            int bt_road_link_id = 1, bt_agent_id = 1;
            for (int i = 0; i < train_path.Rows.Count; i++)
            {
                for (int station = 1; station <= station_num; station++)
                {
                    int[] u_d_t = get_station_time(train_path.Rows[i], station, out int u_tf, out int u_tp, out int d_tf, out int d_tp);
                    int[] u_range = new int[2] { time_sub(u_d_t[0], u_tf), time_add(u_d_t[0], u_tp) };
                    int[] d_range = new int[2] { time_sub(u_d_t[1], d_tf), time_add(u_d_t[1], d_tp) };                  
                    if (u_tf!=0)
                    {
                        DataRow bt_node_u1 = bt_node.NewRow();
                        DataRow bt_node_u2 = bt_node.NewRow();
                        g_bt_node(bt_node_u1, station, u_range[0], 1);
                        g_bt_node(bt_node_u2, station, u_range[1], 1);
                        bt_node.Rows.Add(bt_node_u1); bt_node.Rows.Add(bt_node_u2);
                        DataRow bt_link1 = bt_road_link.NewRow();
                        g_bt_road_link(bt_link1, station, u_range[0], u_range[1], 1, bt_road_link_id++);
                        bt_road_link.Rows.Add(bt_link1);
                        DataRow bt_agent1 = bt_agent.NewRow();
                        g_bt_agent(bt_agent1, station, u_range[0], u_range[1], 1, bt_agent_id++);
                        bt_agent.Rows.Add(bt_agent1);
                    }
                    if (d_tf!=0)
                    {
                        DataRow bt_node_d1 = bt_node.NewRow();
                        DataRow bt_node_d2 = bt_node.NewRow();
                        g_bt_node(bt_node_d1, station, d_range[0], 2);
                        g_bt_node(bt_node_d2, station, d_range[1], 2);
                        bt_node.Rows.Add(bt_node_d1); bt_node.Rows.Add(bt_node_d2);
                        DataRow bt_link2 = bt_road_link.NewRow();
                        g_bt_road_link(bt_link2, station, d_range[0], d_range[1], 2, bt_road_link_id++);
                        bt_road_link.Rows.Add(bt_link2);
                        DataRow bt_agent2 = bt_agent.NewRow();
                        g_bt_agent(bt_agent2, station, d_range[0], d_range[1], 2, bt_agent_id++);
                        bt_agent.Rows.Add(bt_agent2);
                    }                
                }
            }//blocking time
            if (!Directory.Exists(strpath + "\\train_path"))
                Directory.CreateDirectory(strpath + "\\train_path");
            SaveCsv(node, strpath + "\\train_path\\node");
            SaveCsv(road_link, strpath + "\\train_path\\road_link");
            SaveCsv(agent, strpath + "\\train_path\\agent");
            SaveCsv(agent_type, strpath + "\\train_path\\agent_type");
            if (!Directory.Exists(strpath + "\\train_conflicts"))
                Directory.CreateDirectory(strpath + "\\train_conflicts");
            conf_node = GetDistinctSelf(conf_node, "node_id");
            SaveCsv(conf_node, strpath + "\\train_conflicts\\node");
            SaveCsv(conf_road_link, strpath + "\\train_conflicts\\road_link");
            SaveCsv(conf_agent, strpath + "\\train_conflicts\\agent");
            SaveCsv(agent_type, strpath + "\\train_conflicts\\agent_type");
            if (!Directory.Exists(strpath + "\\blocking_time"))
                Directory.CreateDirectory(strpath + "\\blocking_time");
            bt_node = GetDistinctSelf(bt_node, "node_id");
            SaveCsv(bt_node, strpath + "\\blocking_time\\node");
            SaveCsv(bt_road_link, strpath + "\\blocking_time\\road_link");
            SaveCsv(bt_agent, strpath + "\\blocking_time\\agent");
            SaveCsv(agent_type, strpath + "\\blocking_time\\agent_type");
            node.Merge(bt_node);
            for (int i = 0; i < bt_road_link.Rows.Count; i++)
            {
                bt_road_link.Rows[i][1] = road_link_id++;
            }
            road_link.Merge(bt_road_link);
            for (int i = 0; i < bt_agent.Rows.Count; i++)
            {
                bt_agent.Rows[i][0] = agent_id++;
            }
            agent.Merge(bt_agent);
            if (!Directory.Exists(strpath + "\\train_path and blocking_time"))
                Directory.CreateDirectory(strpath + "\\train_path and blocking_time");
            SaveCsv(node, strpath + "\\train_path and blocking_time\\node");
            SaveCsv(road_link, strpath + "\\train_path and blocking_time\\road_link");
            SaveCsv(agent, strpath + "\\train_path and blocking_time\\agent");
            SaveCsv(agent_type, strpath + "\\train_path and blocking_time\\agent_type");
            node.Merge(conf_node);
            for (int i = 0; i < conf_road_link.Rows.Count; i++)
            {
                conf_road_link.Rows[i][1] = road_link_id++;
            }
            road_link.Merge(conf_road_link);
            for (int i = 0; i < conf_agent.Rows.Count; i++)
            {
                conf_agent.Rows[i][0] = agent_id++;
            }
            agent.Merge(conf_agent);
            if (!Directory.Exists(strpath + "\\train_path and blocking_time and train_conflicts"))
                Directory.CreateDirectory(strpath + "\\train_path and blocking_time and train_conflicts");
            SaveCsv(node, strpath + "\\train_path and blocking_time and train_conflicts\\node");
            SaveCsv(road_link, strpath + "\\train_path and blocking_time and train_conflicts\\road_link");
            SaveCsv(agent, strpath + "\\train_path and blocking_time and train_conflicts\\agent");
            SaveCsv(agent_type, strpath + "\\train_path and blocking_time and train_conflicts\\agent_type");
        }
        private void g_train_node(DataRow dr, int station, int time, int zone_id, int node_type)
        {
            dr[1] = station;
            dr[2] = station * 100000 + time;
            dr[3] = zone_id;
            dr[4] = node_type;
            dr[6] = time_sub(time, start_time);
            if ((int)dr[6] >= 100)
            {
                dr[6] = ((int)dr[6] / 100) * 60 + ((int)dr[6] % 100);
            }
            dr[6] = (int)dr[6] * 100;
            dr[7] = 1000 * station;

        }
        private void g_train_link(DataRow dr, int f_s, int t_s, int f_t, int t_t, int id)
        {
            dr[1] = id;
            dr[2] = f_s * 100000 + f_t;
            dr[3] = t_s * 100000 + t_t;
            dr[5] = 1;
            dr[6] = time_sub(t_t, f_t);
            if ((int)dr[6] >= 100)
            {
                dr[6] = ((int)dr[6] / 100) * 60 + ((int)dr[6] % 100);
            }
            for (int i = 7; i <= 10; i++)
            {
                dr[i] = 1;
            }
            dr[11] = dr[6];
        }
        private void g_bt_node(DataRow dr, int station, int time, int flag)
        {
            dr[1] = station;
            if (flag == 2)
            {
                dr[2] = station * 1E7 + 1E5 + time;
                dr[3] = dr[2];//zone_id equal tp node_id
                dr[7] = station * 1000 - 100;

            }
            else
            {
                dr[2] = station * 1e7 + 2e5 + time;
                dr[3] = dr[2];
                dr[7] = station * 1000 + 100;
            }
            dr[4] = 1;
            dr[6] = time_sub(time, start_time);
            if ((int)dr[6] >= 100)
            {
                dr[6] = ((int)dr[6] / 100) * 60 + ((int)dr[6] % 100);
            }
            dr[6] = (int)dr[6] * 100;
        }
        private void g_bt_road_link(DataRow dr, int station, int f_t, int t_t, int flag, int id)
        {
            dr[1] = id;
            if (flag == 2)
            {
                dr[2] = station * 1E7 + 1E5 + f_t;
                dr[3] = station * 1e7 + 1E5 + t_t;
            }
            else
            {
                dr[2] = station * 1E7 + 2E5 + f_t;
                dr[3] = station * 1e7 + 2E5 + t_t;
            }
            dr[5] = 1;
            dr[6] = time_sub(t_t, f_t);
            if ((int)dr[6] >= 100)
            {
                dr[6] = ((int)dr[6] / 100) * 60 + ((int)dr[6] % 100);
            }
            for (int i = 7; i <= 10; i++)
            {
                dr[i] = 1;
            }
            dr[11] = dr[6];

        }
        private void g_bt_agent(DataRow dr, int station, int f_t, int t_t, int flag, int id)
        {
            dr[0] = id;
            if (flag == 2)
            {
                dr[1] = station * 1E7 + 1e5 + f_t;
                dr[2] = station * 1E7 + 1e5 + t_t;
                dr[3] = dr[1];
                dr[4] = dr[2];
                dr[11] = (station * 1E7 + 1e5 + f_t).ToString() + ";" + (station * 1E7 + 1e5 + t_t).ToString();
            }
            else
            {
                dr[1] = station * 1E7 + 2e5 + f_t;
                dr[2] = station * 1E7 + 2e5 + t_t;
                dr[3] = dr[1];
                dr[4] = dr[2];
                dr[11] = (station * 1E7 + 2e5 + f_t).ToString() + ";" + (station * 1E7 + 2e5 + t_t).ToString();
            }
            dr[5] = "blocking time";
            dr[6] = time_int2string(f_t) + "_" + time_int2string(t_t);
            dr[7] = 1;
            dr[8] = time_sub(t_t, f_t);
            if ((int)dr[8] >= 100)
            {
                dr[8] = ((int)dr[8] / 100) * 60 + ((int)dr[8] % 100);
            }
            dr[9] = dr[8];
            dr[10] = dr[8];
            dr[12] = time_int2string(f_t) + ";" + time_int2string(t_t);
        }
        private void g_conf_node(DataRow dr, int station, int time, int flag)
        {
            dr[1] = station;
            if (flag == 2)
            {
                dr[2] = station * 1E7 + 3E5 + time;
                dr[3] = dr[2];//zone_id equal tp node_id
                dr[7] = station * 1000 - 200;

            }
            else
            {
                dr[2] = station * 1e7 + 4e5 + time;
                dr[3] = dr[2];
                dr[7] = station * 1000 + 200;
            }
            dr[4] = 1;
            dr[6] = time_sub(time, start_time);
            if ((int)dr[6] >= 100)
            {
                dr[6] = ((int)dr[6] / 100) * 60 + ((int)dr[6] % 100);
            }
            dr[6] = (int)dr[6] * 100;

        }
        private void g_conf_road_link(DataRow dr, int station, int f_t, int t_t, int flag, int id)
        {
            dr[1] = id;
            if (flag == 2)
            {
                dr[2] = station * 1E7 + 3E5 + f_t;
                dr[3] = station * 1e7 + 3E5 + t_t;
            }
            else
            {
                dr[2] = station * 1E7 + 4E5 + f_t;
                dr[3] = station * 1e7 + 4E5 + t_t;
            }
            dr[5] = 1;
            dr[6] = time_sub(t_t, f_t);
            if ((int)dr[6]>=100)
            {
                dr[6] = ((int)dr[6] / 100)*60 + ((int)dr[6] % 100);
            }
            for (int i = 7; i <= 10; i++)
            {
                dr[i] = 1;
            }
            dr[11] = dr[6];

        }
        private void g_conf_agent(DataRow dr, int station, int f_t, int t_t, int flag, int id)
        {
            dr[0] = id;
            if (flag == 2)
            {
                dr[1] = station * 1E7 + 3e5 + f_t;
                dr[2] = station * 1E7 + 3e5 + t_t;
                dr[3] = dr[1];
                dr[4] = dr[2];
                dr[11] = (station * 1E7 + 3e5 + f_t).ToString() + ";" + (station * 1E7 + 3e5 + t_t).ToString();
            }
            else
            {
                dr[1] = station * 1E7 + 4e5 + f_t;
                dr[2] = station * 1E7 + 4e5 + t_t;
                dr[3] = dr[1];
                dr[4] = dr[2];
                dr[11] = (station * 1E7 + 4e5 + f_t).ToString() + ";" + (station * 1E7 + 4e5 + t_t).ToString();
            }
            dr[5] = "train conflicts";
            dr[6] = time_int2string(f_t) + "_" + time_int2string(t_t);
            dr[7] = 1;
            dr[8] = time_sub(t_t, f_t);
            if ((int)dr[8] >= 100)
            {
                dr[8] = ((int)dr[8] / 100)*60 + ((int)dr[8] % 100);
            }
            dr[9] = dr[8];
            dr[10] = dr[8];
            dr[12] = time_int2string(f_t) + ";" + time_int2string(t_t);
        }
        private List<conf> get_all_conf(DataTable train_path)
        {
            List<conf> confs = new List<conf>();
            int train_num = train_path.Rows.Count;
            for (int i = 0; i < train_num - 1; i++)
            {
                for (int j = i + 1; j < train_num; j++)
                {
                    confs.AddRange(get_2_trains_conf(train_path.Rows[i], train_path.Rows[j]));
                }
            }
            return confs;
        }
        private List<conf> get_2_trains_conf(DataRow dr1, DataRow dr2)
        {
            List<conf> conf_list = new List<conf>();
            for (int now_station = 1; now_station <= station_num; now_station++)
            {
                int[] u_d_t1 = get_station_time(dr1, now_station, out int u_tf1, out int u_tp1, out int d_tf1, out int d_tp1);
                int[] u_d_t2 = get_station_time(dr2, now_station, out int u_tf2, out int u_tp2, out int d_tf2, out int d_tp2);
                int[] u_range1 = new int[2] { time_sub(u_d_t1[0], u_tf1), time_add(u_d_t1[0], u_tp1) };
                int[] u_range2 = new int[2] { time_sub(u_d_t2[0], u_tf2), time_add(u_d_t2[0], u_tp2) };
                int[] d_range1 = new int[2] { time_sub(u_d_t1[1], d_tf1), time_add(u_d_t1[1], d_tp1) };
                int[] d_range2 = new int[2] { time_sub(u_d_t2[1], d_tf2), time_add(u_d_t2[1], d_tp2) };
                if (u_tf1!=0)
                {
                    if (is_range_conf(u_range1, u_range2, out int[] time_range))
                    {
                        conf conf1 = new conf(time_range[0], time_range[1], int.Parse((string)dr1[0]), int.Parse((string)dr2[0]), 1, now_station);
                        conf_list.Add(conf1);
                    }
                }
                if (d_tf1!=0)
                {
                    if (is_range_conf(d_range1, d_range2, out int[] time_range))
                    {
                        conf conf1 = new conf(time_range[0], time_range[1], int.Parse((string)dr1[0]), int.Parse((string)dr2[0]), 2, now_station);
                        conf_list.Add(conf1);
                    }
                }              
            }
            return conf_list;
        }
        private bool is_range_conf(int[] range1, int[] range2, out int[] time_range)
        {
            time_range = new int[2] { 0, 0 };
            if (range1[0] < range2[1] && range1[1] < range2[0])
            {
                return false;
            }
            else if (range2[0] < range1[0] && range2[1] < range1[0])
            {
                return false;
            }
            else
            {
                if (range1[0] <= range2[0] && range1[1] >= range2[0] && range2[1] >= range1[1])
                {
                    time_range = new int[2] { range2[0], range1[1] };
                }
                else if (range2[0] <= range1[0] && range2[1] >= range1[0] && range1[1] >= range2[1])
                {
                    time_range = new int[2] { range1[0], range2[1] };
                }
                else if (range1[0] <= range2[0] && range1[1] >= range2[1])
                {
                    time_range = new int[2] { range2[0], range2[1] };
                }
                else
                {
                    time_range = new int[2] { range1[0], range1[1] };
                }
                return true;
            }
        }
        private int[] get_station_time(DataRow dr, int station, out int u_tf, out int u_tp, out int d_tf, out int d_tp)
        {
            List<int[]> node_seq = trans_node_seq((string)dr[1]);
            List<int> time_seq = trans_time_seq((string)dr[2]);
            int up_time = 0, down_time = 0; u_tf = 0; u_tp = 0; d_tf = 0; d_tp = 0;
            int flag = int.Parse((string)dr[9]);
            for (int i = 0; i < node_seq.Count; i++)
            {
                if (node_seq[i][0] == station)
                {
                    if (station == 1)
                    {
                        up_time = time_seq[i];
                        down_time = 0;
                        if (flag == 1)
                        {
                            u_tf = int.Parse((string)dr[4]);
                            u_tp = int.Parse((string)dr[7]);
                            d_tf = 0; d_tp = 0;
                        }
                        else
                        {
                            u_tf = int.Parse((string)dr[3]);
                            u_tp = int.Parse((string)dr[6]);
                            d_tf = 0; d_tp = 0;
                        }
                    }
                    else if (station == station_num)
                    {
                        up_time = 0;
                        down_time = time_seq[i];
                        if (flag == 1)
                        {
                            d_tf = int.Parse((string)dr[3]);
                            d_tp = int.Parse((string)dr[6]);
                            u_tf = 0; u_tp = 0;
                        }
                        else
                        {
                            d_tf = int.Parse((string)dr[4]);
                            d_tp = int.Parse((string)dr[7]);
                            u_tf = 0; u_tp = 0;
                        }
                    }
                    else
                    {
                        if (node_seq[i][1] == 3)
                        {
                            up_time = time_seq[i];
                            down_time = time_seq[i];
                            u_tf = int.Parse((string)dr[5]);
                            u_tp = int.Parse((string)dr[8]);
                            d_tf = u_tf;
                            d_tp = u_tp;
                        }
                        else
                        {
                            up_time = time_seq[i];
                            down_time = time_seq[i + 1];
                            if (flag == 1)
                            {
                                u_tf = int.Parse((string)dr[4]);
                                u_tp = int.Parse((string)dr[7]);
                                d_tf = int.Parse((string)dr[3]);
                                d_tp = int.Parse((string)dr[6]);
                            }
                            else
                            {
                                u_tf = int.Parse((string)dr[3]);
                                u_tp = int.Parse((string)dr[6]);
                                d_tf = int.Parse((string)dr[4]);
                                d_tp = int.Parse((string)dr[7]);
                            }
                        }
                    }
                    break;
                }
            }
            return new int[2] {  up_time, down_time };
        }
        //conflict struct
        private struct conf
        {
            public int start_time;
            public int end_time;
            public int train_1;
            public int train_2;
            public int station;
            public int flag;
            public conf(int s, int e, int t1, int t2, int flag, int station)
            {
                start_time = s;
                end_time = e;
                train_1 = t1;
                train_2 = t2;
                this.flag = flag;//1 up 2 down
                this.station = station;
            }
        }
        //global var
        int start_time;
        int station_num;
        int time_len;
        //工具函数
        private string time_int2string(int t)
        {
            if (t < 1000)
            {
                return "0" + t.ToString();
            }
            else
            {
                return t.ToString();
            }
        }
        private int time_sub(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;

            int sub_m = m1 - m2;
            int sub_h = h1 - h2;
            if (sub_m < 0)
            {
                return (sub_h - 1) * 100 + (sub_m + 60);
            }
            else
            {
                return sub_h * 100 + sub_m;
            }
        }
        private int time_add(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;
            if (m1 + m2 >= 60)
            {
                return (h1 + h2 + 1) * 100 + (m1 + m2 - 60);
            }
            else
            {
                return (h1 + h2) * 100 + (m1 + m2);
            }
        }
        private DataTable define_node()
        {
            DataTable node = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn phy_node_id = new DataColumn("physical_node_id", typeof(int));
            DataColumn node_id = new DataColumn("node_id", typeof(long));
            DataColumn zone_id = new DataColumn("zone_id", typeof(long));
            DataColumn node_type = new DataColumn("node_type", typeof(int));
            DataColumn control_type = new DataColumn("control_type", typeof(int));
            DataColumn x_coord = new DataColumn("x_coord", typeof(int));
            DataColumn y_coord = new DataColumn("y_coord", typeof(int));
            node.Columns.AddRange(new DataColumn[8] { name, phy_node_id, node_id, zone_id, node_type, control_type, x_coord, y_coord });
            return node;
        }
        private DataTable define_road_link()
        {
            DataTable road_link = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn road_link_id = new DataColumn("road_link_id", typeof(int));
            DataColumn from_node_id = new DataColumn("from_node_id", typeof(long));
            DataColumn to_node_id = new DataColumn("to_node_id", typeof(long));
            DataColumn facility_type = new DataColumn("facility_type", typeof(int));
            DataColumn dir_flag = new DataColumn("dir_flag", typeof(int));
            DataColumn length = new DataColumn("length", typeof(int));
            DataColumn lanes = new DataColumn("lanes", typeof(int));
            DataColumn capacity = new DataColumn("capacity", typeof(int));
            DataColumn free_speed = new DataColumn("free_speed", typeof(int));
            DataColumn link_type = new DataColumn("link_type", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            road_link.Columns.AddRange(new DataColumn[12] { name, road_link_id, from_node_id, to_node_id, facility_type, dir_flag, length, lanes, capacity, free_speed, link_type, cost });
            return road_link;
        }
        private DataTable define_agent()
        {
            DataTable agent = new DataTable();
            DataColumn agent_id = new DataColumn("agent_id", typeof(int));
            DataColumn o_zone_id = new DataColumn("o_zone_id", typeof(long));
            DataColumn d_zone_id = new DataColumn("d_zone_id", typeof(long));
            DataColumn o_node_id = new DataColumn("o_node_id", typeof(long));
            DataColumn d_node_id = new DataColumn("d_node_id", typeof(long));
            DataColumn agent_type = new DataColumn("agent_type", typeof(string));
            DataColumn time_period = new DataColumn("time_period", typeof(string));
            DataColumn volume = new DataColumn("volume", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            DataColumn travel_time = new DataColumn("travel_time", typeof(int));
            DataColumn distance = new DataColumn("distance", typeof(int));
            DataColumn node_sequence = new DataColumn("node_sequence", typeof(string));
            DataColumn time_sequence = new DataColumn("time_sequence", typeof(string));
            agent.Columns.AddRange(new DataColumn[13] { agent_id, o_zone_id, d_zone_id, o_node_id, d_node_id, agent_type, time_period, volume, cost, travel_time, distance, node_sequence, time_sequence });
            return agent;
        }
        private DataTable define_agent_type()
        {
            DataTable agent_type = new DataTable();
            DataColumn agent_type_ = new DataColumn("agent_type", typeof(string));
            DataColumn name = new DataColumn("name", typeof(string));
            agent_type.Columns.Add(agent_type_); agent_type.Columns.Add(name);
            for (int i = 0; i < 3; i++)
            {
                DataRow dr = agent_type.NewRow();
                if (i == 0)
                {
                    dr[0] = "train_path";
                    dr[1] = "train_path";
                }
                else if (i == 1)
                {
                    dr[0] = "train_conflicts";
                    dr[1] = "train_conflicts";
                }
                else
                {
                    dr[0] = "blocking_time";
                    dr[1] = "blocking_time";
                }
                agent_type.Rows.Add(dr);
            }
            return agent_type;
        }
        private int int2HHMM(int t)//t=0 时为start_time
        {
            int hour = t / 60;
            int min = t % 60;
            int s_hour = start_time / 100;
            int s_min = start_time % 100;
            if (min + s_min < 60)
            {
                return (hour + s_hour) * 100 + (min + s_min);
            }
            else
            {
                return (hour + s_hour + 1) * 100 + (min + s_min - 60);
            }
        }
        private int get_zone_id(DataTable zone, int now_station, int now_time)
        {
            if (now_station != 1 && now_station != station_num)
            {
                return 0;
            }
            foreach (DataRow row in zone.Rows)
            {
                int check_station = int.Parse((string)row[3]);
                if (check_station == 0)
                {
                    check_station = 1;
                }
                else
                {
                    check_station = station_num;
                }
                if (now_time >= int.Parse((string)row[1]) && now_time <= int.Parse((string)row[2]) && now_station == check_station)
                {
                    return int.Parse((string)row[0]);
                }
            }
            return 0;
        }
        private int get_node_type(DataTable train_path, int now_station, int now_time)
        {
            if (now_station != 1 && now_station != station_num)
            {
                return 0;
            }
            foreach (DataRow row in train_path.Rows)
            {
                int t0 = trans_time_seq((string)row[2])[0];
                int t1 = trans_time_seq((string)row[2]).Last();
                int flag = int.Parse((string)row[9]);
                if (flag == 1)
                {
                    if (now_station == 1 && now_time == t0)
                    {
                        return 1;
                    }
                    else if (now_station == station_num && now_time == t1)
                    {
                        return 1;
                    }
                }
                else
                {
                    if (now_station == 1 && now_time == t1)
                    {
                        return 1;
                    }
                    else if (now_station == station_num && now_time == t0)
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }
        private List<int[]> trans_node_seq(string node_seq)
        {
            string[] node_seq_list = node_seq.Split(';');
            List<int[]> seq = new List<int[]>();
            for (int i = 0; i < node_seq_list.Length; i++)
            {
                string now_node = node_seq_list[i];
                string[] s_s = now_node.Split('_');
                int[] S_S = new int[2] { int.Parse(s_s[0]), int.Parse(s_s[1]) };
                seq.Add(S_S);
            }
            return seq;
        }
        private List<int> trans_time_seq(string time_seq)
        {
            string[] time_seq_list = time_seq.Split(';');
            List<int> seq = new List<int>();
            for (int i = 0; i < time_seq_list.Length; i++)
            {
                seq.Add(int.Parse(time_seq_list[i]));
            }
            return seq;
        }
        private void SaveCsv(DataTable dt, string filePath)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(filePath + dt.TableName + ".csv", FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(fs, Encoding.Default);
                var data = string.Empty;
                //写出列名称
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    data += dt.Columns[i].ColumnName;
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data);
                //写出各行数据
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    data = string.Empty;
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        data += dt.Rows[i][j].ToString();
                        if (j < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            finally
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }
        }
        private void init(DataTable train_path)
        {
            List<int> ear_time_list = new List<int>(), lat_time_list = new List<int>(), tf_list = new List<int>(), tp_list = new List<int>();
            station_num = 0;
            for (int i = 0; i < train_path.Rows.Count; i++)
            {
                List<int> time_seq = trans_time_seq((string)train_path.Rows[i][2]);
                int tf=int.Parse((string)train_path.Rows[i][4]);
                ear_time_list.Add(time_seq[0]);tf_list.Add(tf);
                int tp= int.Parse((string)train_path.Rows[i][6]);
                lat_time_list.Add(time_seq.Last());tp_list.Add(tp);
            }
            start_time = ear_time_list.Min();int s_index = ear_time_list.IndexOf(start_time);
            int tf_ = tf_list[s_index];
            start_time = time_sub(start_time, tf_);
            int end_time = lat_time_list.Max();int e_index = lat_time_list.IndexOf(end_time);
            int tp_ = tp_list[e_index];
            time_len = time_sub(time_add(end_time, tp_), start_time);
            time_len = (time_len / 100) * 60 + (time_len % 100);
            List<int[]> node_seq = trans_node_seq((string)train_path.Rows[1][1]);
            node_seq.Sort(delegate(int[] x, int[] y) 
            {
                if (x[0] < y[0])
                    return 1;
                else
                    return -1;
            });
            station_num = node_seq[0][0];            
        }
        private DataTable GetDistinctSelf(DataTable SourceDt, string filedName)
        {
            for (int i = SourceDt.Rows.Count - 2; i > 0; i--)
            {
                DataRow[] rows = SourceDt.Select(string.Format("{0}='{1}'", filedName, SourceDt.Rows[i][filedName]));
                if (rows.Length > 1)
                {
                    SourceDt.Rows.RemoveAt(i);
                }
            }
            return SourceDt;


        }
        public DataTable new_train_path()
        {
            DataTable train_path = new DataTable();
            string[] line_list = new string[10] { "train_id", "node_sequence", "time_sequence", "tf for arrival", "tf for departure", "tf for pass", "tp for arrival", "tp for departure", "tp for pass" ,"dir_flag"};
            for (int i = 0; i < 9; i++)
            {
                DataColumn dc = new DataColumn();
                dc.ColumnName = line_list[i];
                dc.DataType = typeof(string);
                train_path.Columns.Add(dc);
            }
            return train_path;
        }
        public DataTable new_zone()
        {
            DataTable zone = new DataTable();
            string[] line_list = new string[4] { "zone_id", "start_time", "end_time", "station" };
            for (int i = 0; i < 4; i++)
            {
                DataColumn dc = new DataColumn();
                dc.ColumnName = line_list[i];
                dc.DataType = typeof(string);
                zone.Columns.Add(dc);
            }
            return zone;
        }
    }
}
